using Community.Core.Entities.UserProfiles;
using Shared.ValueObjects;

namespace Community.Core.Entities.Friendships;

internal sealed class Friendship
{
	public required UserId LowerUserId { get; set; }
	public UserProfile? LowerUser { get; set; }
	public required UserId HigherUserId { get; set; }
	public UserProfile? HigherUser { get; set; }

	public required DateTimeOffset StartedAt { get; set; }
}