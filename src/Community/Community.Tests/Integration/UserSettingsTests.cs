using Community.Core.Entities.UserSettings;
using Community.Core.Features.UserSettings;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class UserSettingsTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task GetWithoutProfileReturnsNotFound()
		=> ResultAssert.Error(await Host.Send(new GetUserSettings(As(new UserId(Guid.NewGuid())))), "UserProfile.NotFound", 404);

	[Fact]
	public async Task UpdateWithoutProfileReturnsNotFoundAndCreatesNothing()
	{
		var userId = new UserId(Guid.NewGuid());

		ResultAssert.Error(
			await Host.Send(new UpdateUserSettings(As(userId), new UpdateUserSettings.Body(GroupInvitePolicy.Nobody))),
			"UserProfile.NotFound", 404);

		Assert.False(await Host.Query(db => db.UserSettings.AnyAsync(x => x.UserId == userId)));
	}

	[Fact]
	public async Task UpdateRecreatesMissingSettingsOfExistingProfile()
	{
		var userId = await Host.CreateUser("alice");
		await Host.Query(db => db.UserSettings.Where(x => x.UserId == userId).ExecuteDeleteAsync());

		ResultAssert.Ok<UserSettingsReadModel>(await Host.Send(new UpdateUserSettings(As(userId), new UpdateUserSettings.Body(GroupInvitePolicy.Nobody))));

		var settings = ResultAssert.Ok<UserSettingsReadModel>(await Host.Send(new GetUserSettings(As(userId))));
		Assert.Equal(GroupInvitePolicy.Nobody, settings.GroupInvitePolicy);
	}

	[Fact]
	public async Task DeletingProfileDeletesSettings()
	{
		var userId = await Host.CreateUser("alice");

		await Host.Query(db => db.UserProfiles.Where(x => x.UserId == userId).ExecuteDeleteAsync());

		Assert.False(await Host.Query(db => db.UserSettings.AnyAsync(x => x.UserId == userId)));
	}
}