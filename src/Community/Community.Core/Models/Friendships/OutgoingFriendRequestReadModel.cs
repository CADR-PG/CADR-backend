using Community.Core.Entities.Friendships;
using Community.Core.Entities.UserProfiles;
using Community.Core.Models.Profiles;
using System.Linq.Expressions;

namespace Community.Core.Models.Friendships;

internal enum OutgoingFriendRequestStatus
{
	Sent,
	Accepted
}

internal sealed record OutgoingFriendRequestReadModel
{
	public required OutgoingFriendRequestStatus Status { get; init; }
	public required DateTimeOffset? SentAt { get; init; }
	public required UserProfileSummaryReadModel Profile { get; init; }

	public static OutgoingFriendRequestReadModel Accepted(UserProfile receiverProfile) => new()
	{
		Status = OutgoingFriendRequestStatus.Accepted,
		SentAt = null,
		Profile = UserProfileSummaryReadModel.Map(receiverProfile)
	};

	public static OutgoingFriendRequestReadModel Sent(DateTimeOffset sentAt, UserProfile receiverProfile) => new()
	{
		Status = OutgoingFriendRequestStatus.Sent,
		SentAt = sentAt,
		Profile = UserProfileSummaryReadModel.Map(receiverProfile)
	};

	public static readonly Expression<Func<FriendRequest, OutgoingFriendRequestReadModel>> MappingExpression = friendRequest =>
		new OutgoingFriendRequestReadModel
		{
			Status = OutgoingFriendRequestStatus.Sent,
			SentAt = friendRequest.SentAt,
			Profile = new UserProfileSummaryReadModel
			{
				UserId = friendRequest.ReceiverId, Name = friendRequest.ReceiverProfile!.Name
			}
		};
}