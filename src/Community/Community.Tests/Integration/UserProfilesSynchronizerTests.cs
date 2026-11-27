using Community.Core.Database;
using Community.Core.Entities.UserSettings;
using Community.Core.Features.UserProfiles;
using Community.Core.Services;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.ValueObjects;
using Users.Contracts;

namespace Community.Tests.Integration;

public sealed class UserProfilesSynchronizerTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task CreatesMissingProfileAndSettings()
	{
		var userId = AddUsersModuleUser("carol.jones@example.com");

		Assert.Equal(1, await Synchronize());

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
		var userId = await Host.CreateUser("dave");
		ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("Dave", "note"))));
		await Host.Query(db => db.UserSettings.Where(x => x.UserId == userId).ExecuteDeleteAsync());

		Assert.Equal(1, await Synchronize());

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Equal("Dave", profile.Name);
		Assert.Equal("note", profile.Note);
		Assert.True(await Host.Query(db => db.UserSettings.AnyAsync(x => x.UserId == userId)));
		Assert.Empty(Host.Users.RequestedUserIds);
	}

	[Fact]
	public async Task FetchesOnlyUsersMissingProfile()
	{
		var synchronizedUserId = await Host.CreateUser("erin");
		var newUserId = AddUsersModuleUser("frank@example.com");

		Assert.Equal(1, await Synchronize());
		Assert.Equal([newUserId], Host.Users.RequestedUserIds);

		Assert.Equal(0, await Synchronize());
		Assert.Single(Host.Users.RequestedUserIds);

		UserId[] userIds = [synchronizedUserId, newUserId];
		Assert.Equal(2, await Host.Query(db => db.UserProfiles.CountAsync(x => userIds.Contains(x.UserId))));
		Assert.Equal(2, await Host.Query(db => db.UserSettings.CountAsync(x => userIds.Contains(x.UserId))));
	}

	[Theory]
	[InlineData(UserProfilesSynchronizer.BatchSize)]
	[InlineData(UserProfilesSynchronizer.BatchSize * 2 + 1)]
	public async Task ProcessesAllPages(int usersCount)
	{
		var userIds = Enumerable.Range(0, usersCount)
			.Select(i => AddUsersModuleUser($"user{i}@example.com"))
			.ToList();

		Assert.Equal(usersCount, await Synchronize());

		Assert.Equal(usersCount, await Host.Query(db => db.UserProfiles.CountAsync(x => userIds.Contains(x.UserId))));
		Assert.Equal(usersCount, await Host.Query(db => db.UserSettings.CountAsync(x => userIds.Contains(x.UserId))));
	}

	private Task<int> Synchronize()
		=> Host.Run(services => new UserProfilesSynchronizer(
				services.GetRequiredService<CommunityDbContext>(),
				services.GetRequiredService<IUsersProvider>(),
				NullLogger<UserProfilesSynchronizer>.Instance)
			.SynchronizeAll(CancellationToken.None));

	private UserId AddUsersModuleUser(string email)
	{
		var userId = new UserId(Guid.NewGuid());
		Host.Users.Add(userId, email);
		return userId;
	}
}