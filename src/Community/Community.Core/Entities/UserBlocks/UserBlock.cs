using Community.Core.Entities.UserProfiles;
using Shared.ValueObjects;

namespace Community.Core.Entities.UserBlocks;

internal sealed class UserBlock
{
	public required UserId BlockerId { get; set; }
	public UserProfile? BlockerProfile { get; set; }

	public required UserId BlockedId { get; set; }
	public UserProfile? BlockedProfile { get; set; }

	public required DateTimeOffset BlockedAt { get; set; }
}