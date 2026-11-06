using Community.Core.Entities.Friendships;
using Community.Core.Entities.UserProfiles;
using Community.Core.Models.Profiles;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Models.Friendships;

internal sealed class FriendReadModel
{
	public required UserProfileSummaryReadModel Profile { get; set; }
	public required DateTimeOffset FriendshipStartedAt { get; set; }

	public static Expression<Func<Friendship, FriendReadModel>> GetMappingExpression(UserId currentUserId) =>
		connection => new FriendReadModel
		{
			Profile = new UserProfileSummaryReadModel
			{
				UserId = connection.LowerUserId == currentUserId
					? connection.HigherUserId
					: connection.LowerUserId,
				Name = connection.LowerUserId == currentUserId
					? connection.HigherUser!.Name
					: connection.LowerUser!.Name
			},
			FriendshipStartedAt = connection.StartedAt
		};

	public static FriendReadModel Map(UserProfile profile, DateTimeOffset startedAt) =>
		new()
		{
			Profile = new UserProfileSummaryReadModel { UserId = profile.UserId, Name = profile.Name },
			FriendshipStartedAt = startedAt
		};
}