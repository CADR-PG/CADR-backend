using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.ValueObjects;
using Users.Contracts;
using UserSettingsEntity = Community.Core.Entities.UserSettings.UserSettings;

namespace Community.Core.Services;

/// <summary>
/// Reconciliation of Community data with the Users module: creates the missing profile and default settings
/// of every user that does not have them (e.g. when the <c>UserRegistered</c> event was lost or dead-lettered).
/// </summary>
/// <remarks>
/// Works in batches through the public API of the Users module (<see cref="IUsersProvider" />):
/// <list type="number">
///   <item>a page of user <b>ids</b> is read from the Users module,</item>
///   <item>a single Community query finds which of them miss a profile or settings,</item>
///   <item>user details are fetched <b>only</b> for users missing a profile (settings need just the id).</item>
/// </list>
/// Idempotent - safe to run concurrently with <see cref="Handlers.UserRegisteredHandler" /> and to re-run.
/// </remarks>
internal sealed partial class UserProfilesSynchronizer(
	CommunityDbContext dbContext,
	IUsersProvider usersProvider,
	ILogger<UserProfilesSynchronizer> logger)
{
	internal const int BatchSize = 1000;
	private const int MaxAttemptsPerBatch = 3;

	/// <returns>Number of users whose profile and/or settings were created.</returns>
	public async Task<int> SynchronizeAll(CancellationToken cancellationToken)
	{
		var synchronizedUsers = 0;
		UserId? after = null;

		while (true)
		{
			var userIds = await usersProvider.GetUserIds(after, BatchSize, cancellationToken);
			if (userIds.Count == 0) break;

			synchronizedUsers += await SynchronizeBatch(userIds, cancellationToken);

			if (userIds.Count < BatchSize) break;
			after = userIds[^1];
		}

		LogSynchronized(synchronizedUsers);
		return synchronizedUsers;
	}

	private async Task<int> SynchronizeBatch(IReadOnlyList<UserId> userIds, CancellationToken cancellationToken)
	{
		for (var attempt = 1; ; attempt++)
		{
			var (missingProfileIds, missingSettingsIds) = await FindMissing(userIds, cancellationToken);
			if (missingProfileIds.Count == 0 && missingSettingsIds.Count == 0) return 0;

			// Users deleted in the meantime are not returned - nothing is created for them.
			var users = await usersProvider.GetUsers(missingProfileIds, cancellationToken);

			foreach (var user in users)
			{
				dbContext.UserProfiles.Add(UserProfile.Create(user.Id, user.Email));
				dbContext.UserSettings.Add(UserSettingsEntity.CreateDefault(user.Id));
			}

			foreach (var userId in missingSettingsIds)
				dbContext.UserSettings.Add(UserSettingsEntity.CreateDefault(userId));

			try
			{
				await dbContext.SaveChangesAsync(cancellationToken);
				return users.Count + missingSettingsIds.Count;
			}
			catch (DbUpdateException exception) when (exception.IsUniqueViolation() && attempt < MaxAttemptsPerBatch)
			{
				// Rows created concurrently (e.g. by the UserRegistered handler) - re-check the batch.
				LogBatchConflict(attempt);
			}
			finally
			{
				dbContext.ChangeTracker.Clear();
			}
		}
	}

	/// <returns>Users without a profile (and so without settings - FK) and users with a profile but without settings.</returns>
	private async Task<(List<UserId> MissingProfileIds, List<UserId> MissingSettingsIds)> FindMissing(
		IReadOnlyList<UserId> userIds, CancellationToken cancellationToken)
	{
		var existingProfiles = await dbContext.UserProfiles
			.AsNoTracking()
			.Where(profile => userIds.Contains(profile.UserId))
			.Select(profile => new
			{
				profile.UserId,
				HasSettings = dbContext.UserSettings.Any(settings => settings.UserId == profile.UserId)
			})
			.ToDictionaryAsync(profile => profile.UserId, profile => profile.HasSettings, cancellationToken);

		var missingProfileIds = userIds.Where(userId => !existingProfiles.ContainsKey(userId)).ToList();
		var missingSettingsIds = existingProfiles.Where(profile => !profile.Value).Select(profile => profile.Key).ToList();

		return (missingProfileIds, missingSettingsIds);
	}

	[LoggerMessage(LogLevel.Information, "User profiles synchronized: created missing data of {Count} users.")]
	private partial void LogSynchronized(int count);

	[LoggerMessage(LogLevel.Warning, "User profiles synchronization batch conflicted with a concurrent insert (attempt {Attempt}), retrying.")]
	private partial void LogBatchConflict(int attempt);
}