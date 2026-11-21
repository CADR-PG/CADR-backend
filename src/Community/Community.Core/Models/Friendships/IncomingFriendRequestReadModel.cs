using Community.Core.Entities.Friendships;
using Community.Core.Models.Profiles;
using System.Linq.Expressions;

namespace Community.Core.Models.Friendships;

internal sealed record IncomingFriendRequestReadModel
{
	public required Guid Id { get; init; }
	public required DateTimeOffset SentAt { get; init; }
	public required UserProfileSummaryReadModel Profile { get; init; }

	public static readonly Expression<Func<FriendRequest, IncomingFriendRequestReadModel>> MappingExpression =
		friendRequest =>
			new IncomingFriendRequestReadModel
			{
				Id = friendRequest.Id,
				SentAt = friendRequest.SentAt,
				Profile = new UserProfileSummaryReadModel
				{
					UserId = friendRequest.SenderId, Name = friendRequest.SenderProfile!.Name
				}
			};
}