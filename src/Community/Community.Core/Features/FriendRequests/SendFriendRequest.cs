using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Friendships;
using Community.Core.Models.Friendships;
using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.FriendRequests;

internal record struct SendFriendRequest(
	CurrentUser CurrentUser,
	SendFriendRequest.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] UserId? ReceiverId);
}

internal sealed class SendFriendRequestEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<SendFriendRequest, SendFriendRequestHandler>("friend-requests/send")
		.AddDataAnnotationsValidation<SendFriendRequest.Body>()
		.Produces<OutgoingFriendRequestReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `InvalidFriendRequest`, `ProfileNotFound`, `FriendRequest.AttemptsExceeded`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class SendFriendRequestHandler(
	FriendshipsService friendshipsService,
	UserProfilesSynchronizer userProfilesSynchronizer,
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<SendFriendRequest>
{
	public async Task<IResult> Handle(
		SendFriendRequest request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var senderId = currentUser.Id;
		var receiverId = payload.ReceiverId!.Value;

		if (senderId == receiverId)
			return new ErrorResult("InvalidFriendRequest", "Invalid friend request.");

		await userProfilesSynchronizer.EnsureSynchronized(receiverId, cancellationToken);

		var receiverProfile = await dbContext.UserProfiles
			.AsNoTracking()
			.FirstOrDefaultAsync(profile => profile.UserId == receiverId, cancellationToken);

		var isBlocked = await dbContext.UserBlocks.Between(senderId, receiverId).AnyAsync(cancellationToken);

		if (receiverProfile is null || isBlocked)
			return new ErrorResult("ProfileNotFound", "No public profile was found for the specified user ID.");

		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(senderId, receiverId);

		var friendshipExists = await dbContext.Friendships
			.AnyAsync(friendship => friendship.LowerUserId == lowerUserId && friendship.HigherUserId == higherUserId, cancellationToken);

		if (friendshipExists)
			return Results.Ok(OutgoingFriendRequestReadModel.Accepted(receiverProfile));

		var receiverPendingRequestExists = await dbContext.FriendRequests
			.AnyAsync(friendRequest => friendRequest.SenderId == receiverId
			                           && friendRequest.ReceiverId == senderId
			                           && friendRequest.Status == FriendRequestStatus.Pending,
				cancellationToken);

		if (receiverPendingRequestExists)
		{
			await friendshipsService.StartFriendship(senderId, receiverId, cancellationToken);
			return Results.Ok(OutgoingFriendRequestReadModel.Accepted(receiverProfile));
		}

		var senderFriendRequest = await dbContext.FriendRequests
			.SingleOrDefaultAsync(friendRequest => friendRequest.SenderId == senderId && friendRequest.ReceiverId == receiverId, cancellationToken);

		if (senderFriendRequest is null)
		{
			senderFriendRequest = FriendRequest.CreateSent(senderId, receiverId, timeProvider);
			await dbContext.AddAsync(senderFriendRequest, cancellationToken);
		}
		else if (!senderFriendRequest.Send(timeProvider))
		{
			return new ErrorResult("FriendRequest.AttemptsExceeded",
				$"Friend request can be sent at most {FriendRequest.MaxAttempts} times.");
		}

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();
			senderFriendRequest = await dbContext.FriendRequests
				.AsNoTracking()
				.SingleAsync(friendRequest => friendRequest.SenderId == senderId && friendRequest.ReceiverId == receiverId, cancellationToken);
		}

		return Results.Ok(OutgoingFriendRequestReadModel.Sent(senderFriendRequest, receiverProfile));
	}
}
