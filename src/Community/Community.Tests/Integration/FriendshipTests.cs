using Community.Core.Entities.Friendships;
using Community.Core.Features.FriendRequests;
using Community.Core.Features.Friends;
using Community.Core.Features.UserBlocks;
using Community.Core.Models.Friendships;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class FriendRequestTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task SendingRequestToSelfIsRejected()
	{
		var alice = await Host.CreateUser("Alice");

		ResultAssert.Error(await Host.Send(new SendFriendRequest(As(alice), new SendFriendRequest.Body(alice))), "InvalidFriendRequest", 400);
	}

	[Fact]
	public async Task SendingRequestToUnknownUserReturnsProfileNotFound()
	{
		var alice = await Host.CreateUser("Alice");

		var result = await Host.Send(new SendFriendRequest(As(alice), new SendFriendRequest.Body(new UserId(Guid.NewGuid()))));

		ResultAssert.Error(result, "ProfileNotFound", 400);
	}

	[Fact]
	public async Task ResendingPendingRequestReturnsSameRequest()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");

		var first = await SendFriendRequest(alice, bob);
		var second = await SendFriendRequest(alice, bob);

		Assert.Equal(first, second);
		var request = await Host.Query(db => db.FriendRequests.SingleAsync(r => r.SenderId == alice && r.ReceiverId == bob));
		Assert.Equal(1u, request.Attempts);
	}

	[Fact]
	public async Task AcceptingRequestCreatesFriendshipForBothUsersAndRemovesRequests()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var requestId = await SendFriendRequest(alice, bob);

		var friend = ResultAssert.Ok<FriendReadModel>(await Host.Send(new AcceptFriendRequest(As(bob), requestId)));

		Assert.Equal(alice.Value, friend.Profile.UserId);
		Assert.Equal([bob.Value], await FriendIdsOf(alice));
		Assert.Equal([alice.Value], await FriendIdsOf(bob));
		Assert.False(await Host.Query(db => db.FriendRequests.AnyAsync(r => r.Id == requestId)));
	}

	[Fact]
	public async Task SenderCannotAcceptOwnRequest()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var requestId = await SendFriendRequest(alice, bob);

		ResultAssert.Error(await Host.Send(new AcceptFriendRequest(As(alice), requestId)), "FriendRequestNotFound", 404);
		Assert.Empty(await FriendIdsOf(alice));
	}

	[Fact]
	public async Task SendingRequestBackToPendingSenderAcceptsIt()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await SendFriendRequest(alice, bob);

		var result = ResultAssert.Ok<OutgoingFriendRequestReadModel>(
			await Host.Send(new SendFriendRequest(As(bob), new SendFriendRequest.Body(alice))));

		Assert.Equal(OutgoingFriendRequestStatus.Accepted, result.Status);
		Assert.Null(result.Id);
		Assert.Equal([bob.Value], await FriendIdsOf(alice));
		Assert.False(await Host.Query(db => db.FriendRequests.AnyAsync(r => r.SenderId == alice && r.ReceiverId == bob)));
	}

	[Fact]
	public async Task SendingRequestToExistingFriendReturnsAccepted()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);

		var result = ResultAssert.Ok<OutgoingFriendRequestReadModel>(
			await Host.Send(new SendFriendRequest(As(alice), new SendFriendRequest.Body(bob))));

		Assert.Equal(OutgoingFriendRequestStatus.Accepted, result.Status);
		Assert.False(await Host.Query(db => db.FriendRequests.AnyAsync(r => r.SenderId == alice && r.ReceiverId == bob)));
	}

	[Fact]
	public async Task RequestCanBeSentAtMostMaxAttemptsTimes()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		for (var attempt = 1; attempt <= FriendRequest.MaxAttempts; attempt++)
		{
			var requestId = await SendFriendRequest(alice, bob);
			ResultAssert.NoContent(await Host.Send(new DeclineFriendRequest(As(bob), requestId)));
		}

		var result = await Host.Send(new SendFriendRequest(As(alice), new SendFriendRequest.Body(bob)));

		ResultAssert.Error(result, "FriendRequest.AttemptsExceeded", 400);
	}

	[Fact]
	public async Task CancelledRequestIsHiddenFromReceiver()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var requestId = await SendFriendRequest(alice, bob);

		ResultAssert.NoContent(await Host.Send(new CancelFriendRequest(As(alice), requestId)));

		var incoming = ResultAssert.Ok<OffsetPaginated<IncomingFriendRequestReadModel>>(
			await Host.Send(new GetIncomingFriendRequests(As(bob), null, null)));
		Assert.Empty(incoming.Items);
	}

	[Fact]
	public async Task ReceiverCancellingRequestIsNoOp()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var requestId = await SendFriendRequest(alice, bob);

		ResultAssert.NoContent(await Host.Send(new CancelFriendRequest(As(bob), requestId)));

		Assert.Equal(FriendRequestStatus.Pending, await StatusOf(requestId));
	}

	[Fact]
	public async Task SenderDecliningOwnRequestIsNoOp()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var requestId = await SendFriendRequest(alice, bob);

		ResultAssert.NoContent(await Host.Send(new DeclineFriendRequest(As(alice), requestId)));

		Assert.Equal(FriendRequestStatus.Pending, await StatusOf(requestId));
	}

	private Task<FriendRequestStatus> StatusOf(Guid requestId)
		=> Host.Query(db => db.FriendRequests.Where(r => r.Id == requestId).Select(r => r.Status).SingleAsync());

	[Fact]
	public async Task RemovingFriendEndsFriendshipForBothUsers()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);

		ResultAssert.NoContent(await Host.Send(new RemoveFriend(As(bob), new RemoveFriend.Body(alice))));

		Assert.Empty(await FriendIdsOf(alice));
		Assert.Empty(await FriendIdsOf(bob));
	}

	private async Task<IEnumerable<Guid>> FriendIdsOf(UserId userId)
	{
		var friends = ResultAssert.Ok<OffsetPaginated<FriendReadModel>>(await Host.Send(new GetFriends(As(userId), null, null)));
		return friends.Items.Select(friend => friend.Profile.UserId);
	}
}

public sealed class UserBlockTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task BlockingSelfIsRejected()
	{
		var alice = await Host.CreateUser("Alice");

		ResultAssert.Error(await Host.Send(new BlockUser(As(alice), new BlockUser.Body(alice))), "UserBlock.InvalidUser", 400);
	}

	[Fact]
	public async Task BlockingIsIdempotent()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await Block(alice, bob);
		var firstBlockedAt = Host.Clock.GetUtcNow();
		Host.Clock.Advance(TimeSpan.FromMinutes(5));

		var second = ResultAssert.Ok<BlockedUserReadModel>(await Host.Send(new BlockUser(As(alice), new BlockUser.Body(bob))));

		Assert.Equal(firstBlockedAt, second.BlockedAt);
		Assert.Equal(1, await Host.Query(db => db.UserBlocks.CountAsync(b => b.BlockerId == alice && b.BlockedId == bob)));
	}

	[Fact]
	public async Task BlockingEndsFriendship()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);

		await Block(alice, bob);

		Assert.False(await Host.Query(db => db.Friendships.AnyAsync(f =>
			(f.LowerUserId == alice && f.HigherUserId == bob) || (f.LowerUserId == bob && f.HigherUserId == alice))));
	}

	[Fact]
	public async Task BlockingClosesPendingRequestsInBothDirectionsAndKeepsAttempts()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var carol = await Host.CreateUser("Carol");
		var aliceToBob = await SendFriendRequest(alice, bob);
		var carolToAlice = await SendFriendRequest(carol, alice);

		await Block(alice, bob);
		await Block(alice, carol);

		var aliceToBobRequest = await Host.Query(db => db.FriendRequests.AsNoTracking().SingleAsync(r => r.Id == aliceToBob));
		var carolToAliceRequest = await Host.Query(db => db.FriendRequests.AsNoTracking().SingleAsync(r => r.Id == carolToAlice));
		Assert.Equal(FriendRequestStatus.Cancelled, aliceToBobRequest.Status);
		Assert.Equal(FriendRequestStatus.Declined, carolToAliceRequest.Status);
		Assert.Equal(1u, aliceToBobRequest.Attempts);
	}

	[Fact]
	public async Task BlockedUserCannotSendFriendRequestInEitherDirection()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await Block(alice, bob);

		ResultAssert.Error(await Host.Send(new SendFriendRequest(As(bob), new SendFriendRequest.Body(alice))), "ProfileNotFound", 400);
		ResultAssert.Error(await Host.Send(new SendFriendRequest(As(alice), new SendFriendRequest.Body(bob))), "ProfileNotFound", 400);
	}

	[Fact]
	public async Task UnblockRemovesBlockButDoesNotRestoreFriendship()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);
		await Block(alice, bob);

		ResultAssert.NoContent(await Host.Send(new UnblockUser(As(alice), new UnblockUser.Body(bob))));

		var blocked = ResultAssert.Ok<OffsetPaginated<BlockedUserReadModel>>(await Host.Send(new GetBlockedUsers(As(alice), null, null)));
		Assert.Empty(blocked.Items);
		Assert.False(await Host.Query(db => db.Friendships.AnyAsync(f =>
			(f.LowerUserId == alice && f.HigherUserId == bob) || (f.LowerUserId == bob && f.HigherUserId == alice))));
	}

	[Fact]
	public async Task OnlyBlockerSeesBlockInBlockedList()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await Block(alice, bob);

		var alicesList = ResultAssert.Ok<OffsetPaginated<BlockedUserReadModel>>(await Host.Send(new GetBlockedUsers(As(alice), null, null)));
		var bobsList = ResultAssert.Ok<OffsetPaginated<BlockedUserReadModel>>(await Host.Send(new GetBlockedUsers(As(bob), null, null)));

		Assert.Equal([bob.Value], alicesList.Items.Select(item => item.Profile.UserId));
		Assert.Empty(bobsList.Items);
	}
}