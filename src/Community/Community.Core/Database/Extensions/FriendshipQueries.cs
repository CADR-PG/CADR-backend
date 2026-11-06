using Community.Core.Entities.Friendships;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Community.Core.Database.Extensions;

internal static class FriendshipQueries
{
	public static Task<FriendRequest?> GetOutgoingPendingFriendRequest(this IQueryable<FriendRequest> query,
		Guid friendRequestId,
		UserId senderId, CancellationToken cancellationToken = default)
		=> query.FirstOrDefaultAsync(
			friendRequest => friendRequest.Id == friendRequestId && friendRequest.SenderId == senderId &&
			                 friendRequest.Status == FriendRequestStatus.Pending, cancellationToken);

	public static Task<FriendRequest?> GetIncomingPendingFriendRequest(this IQueryable<FriendRequest> dbSet,
		Guid friendRequestId,
		UserId receiverId, CancellationToken cancellationToken = default)
		=> dbSet.FirstOrDefaultAsync(
			friendRequest => friendRequest.Id == friendRequestId && friendRequest.ReceiverId == receiverId &&
			                 friendRequest.Status == FriendRequestStatus.Pending, cancellationToken);
}