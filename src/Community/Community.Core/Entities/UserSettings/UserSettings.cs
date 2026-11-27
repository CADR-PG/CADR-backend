using Shared.ValueObjects;

namespace Community.Core.Entities.UserSettings;

internal sealed class UserSettings
{
	public required UserId UserId { get; set; }

	public GroupInvitePolicy GroupInvitePolicy { get; set; }

	public static UserSettings CreateDefault(UserId userId) => new()
	{
		UserId = userId,
		GroupInvitePolicy = GroupInvitePolicy.Everyone
	};
}

public enum GroupInvitePolicy
{
	Everyone = 0,
	FriendsOnly = 1,
	Nobody = 2
}