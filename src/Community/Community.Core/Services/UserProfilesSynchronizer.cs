using Community.Core.Database;
using Community.Core.Database.Configurations;
using Community.Core.Entities.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Community.Core.Services;

internal sealed class UserProfilesSynchronizer(CommunityDbContext dbContext, IMemoryCache cache)
{
	private static readonly TimeSpan SynchronizedUserLifetime = TimeSpan.FromMinutes(30);
	private static readonly TimeSpan FullSynchronizationInterval = TimeSpan.FromMinutes(1);
	private const string FullSynchronizationCacheKey = "Community:UserProfiles:FullSynchronization";

	public async Task EnsureSynchronized(IEnumerable<UserId> userIds, CancellationToken cancellationToken)
	{
		var notSynchronizedIds = userIds
			.Distinct()
			.Where(userId => !cache.TryGetValue(GetCacheKey(userId), out _))
			.ToList();

		if (notSynchronizedIds.Count == 0) return;

		var idsParameter = new NpgsqlParameter("ids", notSynchronizedIds.Select(userId => userId.Value).ToArray());
		await dbContext.Database.ExecuteSqlRawAsync(BuildUpsertSql(onlySelectedUsers: true), [idsParameter], cancellationToken);

		var synchronizedIds = await dbContext.UserProfiles
			.AsNoTracking()
			.Where(userProfile => notSynchronizedIds.Contains(userProfile.UserId))
			.Select(userProfile => userProfile.UserId)
			.ToListAsync(cancellationToken);

		foreach (var userId in synchronizedIds)
			cache.Set(GetCacheKey(userId), true, SynchronizedUserLifetime);
	}

	public Task EnsureSynchronized(UserId userId, CancellationToken cancellationToken)
		=> EnsureSynchronized([userId], cancellationToken);

	public async Task EnsureAllSynchronized(CancellationToken cancellationToken)
	{
		if (cache.TryGetValue(FullSynchronizationCacheKey, out _)) return;

		cache.Set(FullSynchronizationCacheKey, true, FullSynchronizationInterval);

		try
		{
			await dbContext.Database.ExecuteSqlRawAsync(BuildUpsertSql(onlySelectedUsers: false), cancellationToken);
		}
		catch
		{
			cache.Remove(FullSynchronizationCacheKey);
			throw;
		}
	}

	private static string GetCacheKey(UserId userId) => $"Community:UserProfiles:{userId}";

	private string BuildUpsertSql(bool onlySelectedUsers)
	{
		var profileEntity = dbContext.Model.FindEntityType(typeof(UserProfile))!;
		var profileTable = StoreObjectIdentifier.Table(profileEntity.GetTableName()!, profileEntity.GetSchema());
		string ProfileColumn(string property) => Quote(profileEntity.FindProperty(property)!.GetColumnName(profileTable)!);

		var profileId = ProfileColumn(nameof(UserProfile.UserId));
		var profileFirstName = ProfileColumn(nameof(UserProfile.FirstName));
		var profileLastName = ProfileColumn(nameof(UserProfile.LastName));
		var profileName = ProfileColumn(nameof(UserProfile.Name));
		var profileIsNameCustomized = ProfileColumn(nameof(UserProfile.IsNameCustomized));

		var userEntity = dbContext.Model.FindEntityType(typeof(UserReference))!;
		var userTable = StoreObjectIdentifier.Table(userEntity.GetTableName()!, userEntity.GetSchema());
		string UserColumn(string property) => Quote(userEntity.FindProperty(property)!.GetColumnName(userTable)!);

		var userId = UserColumn(nameof(UserReference.Id));
		var firstName = UserColumn(nameof(UserReference.FirstName));
		var lastName = UserColumn(nameof(UserReference.LastName));

		const int maxLength = UserProfileConfiguration.MaxNameLength;
		var filter = onlySelectedUsers ? $"WHERE u.{userId} = ANY(@ids)" : string.Empty;

		return $"""
			INSERT INTO {QuoteTable(profileTable)} AS p ({profileId}, {profileFirstName}, {profileLastName}, {profileName}, {profileIsNameCustomized})
			SELECT u.{userId},
			       left(btrim(u.{firstName}), {maxLength}),
			       left(btrim(u.{lastName}), {maxLength}),
			       left(btrim(concat_ws(' ', btrim(u.{firstName}), btrim(u.{lastName}))), {maxLength}),
			       FALSE
			FROM {QuoteTable(userTable)} AS u
			{filter}
			ON CONFLICT ({profileId}) DO UPDATE SET
				{profileFirstName} = EXCLUDED.{profileFirstName},
				{profileLastName} = EXCLUDED.{profileLastName},
				{profileName} = CASE WHEN p.{profileIsNameCustomized} THEN p.{profileName} ELSE EXCLUDED.{profileName} END
			WHERE p.{profileFirstName} IS DISTINCT FROM EXCLUDED.{profileFirstName}
			   OR p.{profileLastName} IS DISTINCT FROM EXCLUDED.{profileLastName}
			   OR (NOT p.{profileIsNameCustomized} AND p.{profileName} IS DISTINCT FROM EXCLUDED.{profileName})
			""";
	}

	private static string QuoteTable(StoreObjectIdentifier table)
		=> table.Schema is null ? Quote(table.Name) : $"{Quote(table.Schema)}.{Quote(table.Name)}";

	private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}