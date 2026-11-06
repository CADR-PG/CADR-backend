using Community.Core.Database;
using Community.Core.Entities.Friendships;
using Community.Core.Models.Friendships;
using Community.Core.Models.Profiles;
using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.ValueObjects;

namespace Community.Core.Features.FriendRequests;

internal record struct SendFriendRequest(
	CurrentUser CurrentUser,
	SendFriendRequest.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid ReceiverId);
}

internal sealed class SendFriendRequestEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<SendFriendRequest, SendFriendRequestHandler>("friend-requests/send")
		.Produces<OutgoingFriendRequestReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`InvalidFriendRequest`, `ProfileNotFound`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class SendFriendRequestHandler(
	FriendshipsService friendshipsService,
	CommunityDbContext dbContext
) : IHttpRequestHandler<SendFriendRequest>
{
	public async Task<IResult> Handle(
		SendFriendRequest request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var senderId = currentUser.Id;
		var receiverId = new UserId(payload.ReceiverId);

		if (senderId == receiverId)
			return new ErrorResult("InvalidFriendRequest",
				"Invalid friend request.");

		var receiverProfile = await dbContext.UserProfiles
			.AsNoTracking()
			.FirstOrDefaultAsync(profile => profile.UserId == receiverId, cancellationToken);

		if (receiverProfile is null)
			return new ErrorResult(
				"ProfileNotFound",
				"No public profile was found for the specified user ID.");

		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(senderId, receiverId);

		var friendship = await dbContext.Friendships
			.Include(x => x.HigherUserId)
			.SingleOrDefaultAsync(
				friendship =>
					friendship.LowerUserId == lowerUserId &&
					friendship.HigherUserId == higherUserId,
				cancellationToken);

		if (friendship is not null)
			return Results.Ok(OutgoingFriendRequestReadModel.Accepted(receiverProfile));

		// TODO: ADD DISTRIBUTED LOCKING, TRANSACTIONS...

		var receiverFriendshipRequestExists = await dbContext.FriendRequests
			.AnyAsync(
				friendshipRequest => friendshipRequest.SenderId == receiverId && friendshipRequest.ReceiverId == senderId,
				cancellationToken);

		if (receiverFriendshipRequestExists)
		{
			await friendshipsService.StartFriendship(senderId, receiverId);
			return Results.Ok(OutgoingFriendRequestReadModel.Accepted(receiverProfile));
		}

		var senderFriendshipRequest = await dbContext.FriendRequests
			.SingleOrDefaultAsync(
				friendshipRequest => friendshipRequest.SenderId == senderId && friendshipRequest.ReceiverId == receiverId,
				cancellationToken);

		if (senderFriendshipRequest is null)
		{
			senderFriendshipRequest = FriendRequest.CreateSent(senderId, receiverId);
			await dbContext.AddAsync(senderFriendshipRequest, cancellationToken);
		}
		else
		{
			senderFriendshipRequest.Send();
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(OutgoingFriendRequestReadModel.Sent(senderFriendshipRequest.SentAt, receiverProfile));
	}
}