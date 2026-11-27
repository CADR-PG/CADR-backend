using Community.Core.Database.Configurations;
using Community.Core.Entities.UserSettings;
using Community.Core.Features.UserProfiles;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class UserRegisteredHandlerTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task CreatesProfileNamedAfterEmailAndDefaultSettings()
	{
		var userId = RegisterInUsersModule("alice.smith@example.com");

		await Host.ConsumeUserRegistered(userId);

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Null(profile.FirstName);
		Assert.Null(profile.LastName);
		Assert.Equal("alice.smith", profile.Name);
		Assert.Null(profile.Note);

		var settings = await Host.Query(db => db.UserSettings.SingleAsync(x => x.UserId == userId));
		Assert.Equal(GroupInvitePolicy.Everyone, settings.GroupInvitePolicy);
	}

	[Fact]
	public async Task RedeliveredEventIsIgnored()
	{
		var userId = RegisterInUsersModule("alice@example.com");

		await Host.ConsumeUserRegistered(userId);
		await Host.ConsumeUserRegistered(userId);

		Assert.Equal(1, await Host.Query(db => db.UserProfiles.CountAsync(x => x.UserId == userId)));
		Assert.Equal(1, await Host.Query(db => db.UserSettings.CountAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task UnknownUserIsIgnored()
	{
		var userId = new UserId(Guid.NewGuid());

		await Host.ConsumeUserRegistered(userId);

		Assert.False(await Host.Query(db => db.UserProfiles.AnyAsync(x => x.UserId == userId)));
		Assert.False(await Host.Query(db => db.UserSettings.AnyAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task RedeliveredEventKeepsCustomizedProfile()
	{
		var userId = await Host.CreateUser("alice");
		ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("Ally", null))));

		await Host.ConsumeUserRegistered(userId);

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Equal("Ally", profile.Name);
		Assert.Equal(1, await Host.Query(db => db.UserSettings.CountAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task KeepsExistingSettings()
	{
		var userId = RegisterInUsersModule("alice@example.com");
		await Host.ConsumeUserRegistered(userId);
		await Host.Query(db => db.UserSettings
			.Where(x => x.UserId == userId)
			.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.GroupInvitePolicy, GroupInvitePolicy.Nobody)));

		await Host.ConsumeUserRegistered(userId);

		var settings = await Host.Query(db => db.UserSettings.SingleAsync(x => x.UserId == userId));
		Assert.Equal(GroupInvitePolicy.Nobody, settings.GroupInvitePolicy);
	}

	[Fact]
	public async Task TruncatesNameToMaxLength()
	{
		var userId = RegisterInUsersModule($"{new string('a', UserProfileConfiguration.MaxNameLength + 10)}@example.com");

		await Host.ConsumeUserRegistered(userId);

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(await Host.Send(new GetMyUserProfile(As(userId))));
		Assert.Equal(new string('a', UserProfileConfiguration.MaxNameLength), profile.Name);
	}

	private UserId RegisterInUsersModule(string email)
	{
		var userId = new UserId(Guid.NewGuid());
		Host.Users.Add(userId, email);
		return userId;
	}
}