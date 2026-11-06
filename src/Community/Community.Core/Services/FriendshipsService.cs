using Community.Core.Database;
using Community.Core.Entities.Friendships;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Community.Core.Services;

internal sealed class FriendshipsService(CommunityDbContext dbContext)
{
	public Task<Friendship> StartFriendship(FriendRequest friendRequest)
		=> StartFriendship(friendRequest.SenderId, friendRequest.ReceiverId);

	public async Task<Friendship> StartFriendship(UserId firstUserId, UserId secondUserId)
	{
		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(firstUserId, secondUserId);

		var connection = new Friendship
		{
			LowerUserId = lowerUserId,
			HigherUserId = higherUserId,
			StartedAt = DateTimeOffset.UtcNow
		};

		await using var transaction = await dbContext.Database.BeginTransactionAsync();

		await dbContext.FriendRequests
			.Where(x => x.SenderId == firstUserId && x.ReceiverId == secondUserId
			            || x.SenderId == secondUserId && x.ReceiverId == firstUserId)
			.ExecuteDeleteAsync();

		dbContext.Add(connection);
		await dbContext.SaveChangesAsync();

		await transaction.CommitAsync();

		return connection;
	}

	public async Task EndFriendshipIfExists(UserId firstUserId, UserId secondUserId, CancellationToken cancellationToken)
	{
		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(firstUserId, secondUserId);

		await dbContext.Friendships
			.Where(friendship => friendship.LowerUserId == lowerUserId || friendship.HigherUserId == higherUserId)
			.ExecuteDeleteAsync(cancellationToken);
	}
}