using Shared.ValueObjects;

namespace Community.Core.Entities.UserSettings;

internal sealed class UserSettings
{
	public required UserId UserId { get; set; }

	public GroupInvitePolicy GroupInvitePolicy { get; set; }

}

public enum GroupInvitePolicy
{
	Everyone = 0,
	FriendsOnly = 1,
	Nobody = 2
}