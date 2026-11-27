using Community.Core.Entities.UserSettings;
using Community.Core.Features.UserProfiles;
using Community.Core.Services;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class UserProfilesSynchronizerTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task CreatesMissingProfileAndSettings()
	{
		var userId = await InsertUsersModuleUser("carol.jones@example.com");

		await Synchronize();

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Equal("carol.jones", profile.Name);
		Assert.Null(profile.FirstName);
		Assert.Null(profile.LastName);
		var settings = await Host.Query(db => db.UserSettings.SingleAsync(x => x.UserId == userId));
		Assert.Equal(GroupInvitePolicy.Everyone, settings.GroupInvitePolicy);
	}

	[Fact]
	public async Task CreatesOnlyMissingSettingsAndKeepsExistingProfile()
	{
		var userId = await InsertUsersModuleUser("dave@example.com");
		Host.Users.Add(userId, "dave@example.com");
		await Host.ConsumeUserRegistered(userId);
		ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("Dave", "note"))));
		await Host.Query(db => db.UserSettings.Where(x => x.UserId == userId).ExecuteDeleteAsync());

		await Synchronize();

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Equal("Dave", profile.Name);
		Assert.Equal("note", profile.Note);
		Assert.True(await Host.Query(db => db.UserSettings.AnyAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task IsIdempotent()
	{
		var userId = await InsertUsersModuleUser("erin@example.com");

		await Synchronize();
		Assert.Equal(0, await Synchronize());

		Assert.Equal(1, await Host.Query(db => db.UserProfiles.CountAsync(x => x.UserId == userId)));
		Assert.Equal(1, await Host.Query(db => db.UserSettings.CountAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task ProcessesAllBatches()
	{
		var userIds = new List<UserId>();
		for (var i = 0; i < UserProfilesSynchronizer.BatchSize * 2 + 1; i++)
			userIds.Add(new UserId(Guid.NewGuid()));

		await Host.Query(db => db.Database.ExecuteSqlRawAsync(
			"""INSERT INTO "Users"."Users" ("Id", "Email") SELECT id, id::text || '@example.com' FROM unnest({0}::uuid[]) AS id""",
			userIds.Select(userId => userId.Value).ToArray()));

		await Synchronize();

		var ids = userIds.ToList();
		Assert.Equal(ids.Count, await Host.Query(db => db.UserProfiles.CountAsync(x => ids.Contains(x.UserId))));
		Assert.Equal(ids.Count, await Host.Query(db => db.UserSettings.CountAsync(x => ids.Contains(x.UserId))));
	}

	private Task<int> Synchronize()
		=> Host.Query(db => new UserProfilesSynchronizer(db, NullLogger<UserProfilesSynchronizer>.Instance).SynchronizeAll(CancellationToken.None));

	private async Task<UserId> InsertUsersModuleUser(string email)
	{
		var userId = new UserId(Guid.NewGuid());
		await Host.Query(db => db.Database.ExecuteSqlRawAsync(
			"""INSERT INTO "Users"."Users" ("Id", "Email") VALUES ({0}, {1})""", userId.Value, email));
		return userId;
	}
}