using Shared.ValueObjects;

namespace Community.Core.Entities.UserProfiles;

internal sealed class UserProfile
{
	public required UserId UserId { get; set; }
	public required string Name { get; set; }
}