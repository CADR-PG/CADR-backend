using Community.Core.Database;
using Community.Core.Entities.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Users.Contracts;
using Users.Contracts.Events;
using UserSettingsEntity = Community.Core.Entities.UserSettings.UserSettings;

namespace Community.Core.Handlers;

/// <summary>
/// Creates the community profile and default settings of a newly registered user (runs in the message broker).
/// </summary>
/// <remarks>
/// Transactions and retries are configured fluently for the whole message broker
/// (<c>Shared.Messaging.Extensions.ApplyMessageHandlingPolicies</c>): Wolverine's EF Core middleware saves the changes.
/// </remarks>
public static class UserRegisteredHandler
{
	public static async Task Handle(
		UserRegistered integrationEvent,
		IUsersProvider usersProvider,
		CommunityDbContext dbContext,
		CancellationToken cancellationToken)
	{
		var userId = integrationEvent.UserId;

		// Idempotent: the event may be redelivered. A concurrent insert ends with a unique violation,
		// and the retry sees the existing rows.
		if (!await dbContext.UserProfiles.AnyAsync(profile => profile.UserId == userId, cancellationToken))
		{
			// The event is published only after the user is committed, so a missing user means
			// the account no longer exists - there is nothing to create.
			var user = await usersProvider.GetUser(userId, cancellationToken);
			if (user is null) return;

			dbContext.UserProfiles.Add(UserProfile.Create(userId, user.Email));
		}

		if (!await dbContext.UserSettings.AnyAsync(settings => settings.UserId == userId, cancellationToken))
			dbContext.UserSettings.Add(UserSettingsEntity.CreateDefault(userId));
	}
}