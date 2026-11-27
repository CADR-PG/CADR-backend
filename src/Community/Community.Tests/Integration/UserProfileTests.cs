using Community.Core.Features.UserProfiles;
using Community.Core.Models.Profiles;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Services;

namespace Community.Tests.Integration;

public sealed class UserProfileTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task UnchangedEditDoesNotCountTowardsDailyLimit()
	{
		var userId = await Host.CreateUser("Alice", "Smith");

		var profile = ResultAssert.Ok<MyUserProfileReadModel>(
			await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("Alice.Smith", null))));

		Assert.Equal(UserProfileLimit, profile.EditLimit.RemainingEdits);
		Assert.Equal(0, await Host.Query(db => db.UserProfileEdits.CountAsync(edit => edit.UserId == userId)));
	}

	[Fact]
	public async Task EditsAreLimitedPerUtcDayAndResetAtMidnight()
	{
		var userId = await Host.CreateUser("Alice");
		for (var edit = 1; edit <= UserProfileLimit; edit++)
		{
			var profile = ResultAssert.Ok<MyUserProfileReadModel>(
				await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body($"Name {edit}", $"note {edit}"))));
			Assert.Equal(UserProfileLimit - edit, profile.EditLimit.RemainingEdits);
		}

		var limited = await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("One too many", null)));
		ResultAssert.Error(limited, "UserProfile.EditLimitReached", 429);

		var nextMidnight = new DateTimeOffset(Host.Clock.GetUtcNow().UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
		Host.Clock.SetUtcNow(nextMidnight);

		var afterReset = ResultAssert.Ok<MyUserProfileReadModel>(
			await Host.Send(new UpdateMyUserProfile(As(userId), new UpdateMyUserProfile.Body("Next day", null))));
		Assert.Equal("Next day", afterReset.Name);
		Assert.Equal(UserProfileLimit - 1, afterReset.EditLimit.RemainingEdits);
		Assert.Equal(nextMidnight.AddDays(1), afterReset.EditLimit.ResetsAt);
	}

	[Fact]
	public async Task SearchExcludesSelfAndUsersBlockedInEitherDirection()
	{
		var marker = Guid.NewGuid().ToString("N")[..10];
		var me = await Host.CreateUser("Me", marker);
		var visible = await Host.CreateUser("Visible", marker);
		var blockedByMe = await Host.CreateUser("BlockedByMe", marker);
		var blockingMe = await Host.CreateUser("BlockingMe", marker);
		await Block(me, blockedByMe);
		await Block(blockingMe, me);

		var page = ResultAssert.Ok<OffsetPaginated<UserProfileSummaryReadModel>>(
			await Host.Send(new GetPaginatedUserProfiles(As(me), marker)));

		Assert.Equal([visible.Value], page.Items.Select(item => item.UserId));
	}

	[Theory]
	[InlineData("%")]
	[InlineData("_")]
	public async Task SearchTreatsLikeWildcardsLiterally(string wildcard)
	{
		var me = await Host.CreateUser("Me");
		await Host.CreateUser("Plain", "Name");

		var page = ResultAssert.Ok<OffsetPaginated<UserProfileSummaryReadModel>>(
			await Host.Send(new GetPaginatedUserProfiles(As(me), wildcard)));

		Assert.Empty(page.Items);
	}

	private const int UserProfileLimit = Core.Entities.UserProfiles.UserProfile.MaxEditsPerDay;
}