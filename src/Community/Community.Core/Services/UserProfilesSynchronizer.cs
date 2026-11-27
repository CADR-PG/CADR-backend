using Community.Core.Database;
using Community.Core.Entities.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Users.Contracts.Database.References;
using UserSettingsEntity = Community.Core.Entities.UserSettings.UserSettings;

namespace Community.Core.Services;

/// <summary>
/// Reconciliation of Community data with the Users module: creates the missing profile and default settings
/// of every user that does not have them (e.g. existing accounts, or when the <c>UserRegistered</c> event was lost or dead-lettered).
/// </summary>
/// <remarks>
/// Idempotent - safe to run concurrently with <see cref="Handlers.UserRegisteredHandler" /> and to re-run.
/// Users are read through <see cref="UserReference" /> (read-only mapping of the Users module table).
/// </remarks>
internal sealed partial class UserProfilesSynchronizer(CommunityDbContext dbContext, ILogger<UserProfilesSynchronizer> logger)
{
	internal const int BatchSize = 500;
	private const int MaxConsecutiveConflicts = 5;

	/// <returns>Number of users whose profile and/or settings were created.</returns>
	public async Task<int> SynchronizeAll(CancellationToken cancellationToken)
	{
		var synchronizedUsers = 0;
		var consecutiveConflicts = 0;

		while (true)
		{
			// Created rows drop out of the query, so the next batch always starts from the beginning.
			var batch = await dbContext.Set<UserReference>()
				.AsNoTracking()
				.Select(user => new
				{
					user.Id,
					user.Email,
					HasProfile = dbContext.UserProfiles.Any(profile => profile.UserId == user.Id),
					HasSettings = dbContext.UserSettings.Any(settings => settings.UserId == user.Id)
				})
				.Where(user => !user.HasProfile || !user.HasSettings)
				.OrderBy(user => user.Id)
				.Take(BatchSize)
				.ToListAsync(cancellationToken);

			if (batch.Count == 0) break;

			foreach (var user in batch)
			{
				if (!user.HasProfile)
					dbContext.UserProfiles.Add(UserProfile.Create(user.Id, user.Email));

				if (!user.HasSettings)
					dbContext.UserSettings.Add(UserSettingsEntity.CreateDefault(user.Id));
			}

			try
			{
				await dbContext.SaveChangesAsync(cancellationToken);
				synchronizedUsers += batch.Count;
				consecutiveConflicts = 0;
			}
			catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
													  && ++consecutiveConflicts <= MaxConsecutiveConflicts)
			{
				// Rows created concurrently (e.g. by the UserRegistered handler) - re-read the batch.
				LogBatchConflict(consecutiveConflicts);
			}
			finally
			{
				dbContext.ChangeTracker.Clear();
			}
		}

		LogSynchronized(synchronizedUsers);
		return synchronizedUsers;
	}

	[LoggerMessage(LogLevel.Information, "User profiles synchronized: created missing data of {Count} users.")]
	private partial void LogSynchronized(int count);

	[LoggerMessage(LogLevel.Warning, "User profiles synchronization batch conflicted with a concurrent insert (attempt {Attempt}), retrying.")]
	private partial void LogBatchConflict(int attempt);
}