using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Friendships;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Community.Core.Services;

internal sealed class FriendshipsService(CommunityDbContext dbContext, TimeProvider timeProvider)
{
	public Task<Friendship> StartFriendship(FriendRequest friendRequest, CancellationToken cancellationToken)
		=> StartFriendship(friendRequest.SenderId, friendRequest.ReceiverId, cancellationToken);

	public async Task<Friendship> StartFriendship(UserId firstUserId, UserId secondUserId, CancellationToken cancellationToken)
	{
		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(firstUserId, secondUserId);

		var friendship = new Friendship
		{
			LowerUserId = lowerUserId,
			HigherUserId = higherUserId,
			StartedAt = timeProvider.GetUtcNow()
		};

		try
		{
			await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

			await DeleteFriendRequestsBetween(firstUserId, secondUserId, cancellationToken);

			dbContext.Add(friendship);
			await dbContext.SaveChangesAsync(cancellationToken);

			await transaction.CommitAsync(cancellationToken);
			return friendship;
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();

			return await dbContext.Friendships
				.AsNoTracking()
				.FirstAsync(x => x.LowerUserId == lowerUserId && x.HigherUserId == higherUserId, cancellationToken);
		}
	}

	public async Task EndFriendshipIfExists(UserId firstUserId, UserId secondUserId, CancellationToken cancellationToken)
	{
		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(firstUserId, secondUserId);

		await dbContext.Friendships
			.Where(friendship => friendship.LowerUserId == lowerUserId && friendship.HigherUserId == higherUserId)
			.ExecuteDeleteAsync(cancellationToken);
	}

	public Task<int> DeleteFriendRequestsBetween(UserId firstUserId, UserId secondUserId, CancellationToken cancellationToken)
		=> dbContext.FriendRequests
			.Where(friendRequest => (friendRequest.SenderId == firstUserId && friendRequest.ReceiverId == secondUserId)
			                        || (friendRequest.SenderId == secondUserId && friendRequest.ReceiverId == firstUserId))
			.ExecuteDeleteAsync(cancellationToken);

	public async Task ClosePendingFriendRequestsOnBlock(UserId blockerId, UserId blockedId, CancellationToken cancellationToken)
	{
		await dbContext.FriendRequests
			.Where(friendRequest => friendRequest.SenderId == blockerId
			                        && friendRequest.ReceiverId == blockedId
			                        && friendRequest.Status == FriendRequestStatus.Pending)
			.ExecuteUpdateAsync(setters => setters.SetProperty(friendRequest => friendRequest.Status, FriendRequestStatus.Cancelled), cancellationToken);

		await dbContext.FriendRequests
			.Where(friendRequest => friendRequest.SenderId == blockedId
			                        && friendRequest.ReceiverId == blockerId
			                        && friendRequest.Status == FriendRequestStatus.Pending)
			.ExecuteUpdateAsync(setters => setters.SetProperty(friendRequest => friendRequest.Status, FriendRequestStatus.Declined), cancellationToken);
	}
}
