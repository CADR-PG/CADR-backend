using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Models.Friendships;
using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;

namespace Community.Core.Features.FriendRequests;

internal record struct AcceptFriendRequest(
	CurrentUser CurrentUser,
	[FromRoute] Guid FriendRequestId
) : IHttpRequest;

internal sealed class AcceptFriendRequestEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AcceptFriendRequest, AcceptFriendRequestHandler>("friend-requests/{friendRequestId:guid}/accept")
		.Produces<FriendReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`FriendRequestNotFound`");
}

internal sealed class AcceptFriendRequestHandler(
	CommunityDbContext dbContext,
	FriendshipsService friendshipsService
) : IHttpRequestHandler<AcceptFriendRequest>
{
	public async Task<IResult> Handle(
		AcceptFriendRequest request,
		CancellationToken cancellationToken)
	{
		var (currentUser, friendRequestId) = request;

		var receiverId = currentUser.Id;

		var friendRequest = await dbContext.FriendRequests
			.Include(x => x.SenderProfile)
			.AsNoTracking()
			.GetIncomingPendingFriendRequest(friendRequestId, receiverId, cancellationToken);

		if (friendRequest is null)
			return new ErrorResult("FriendRequestNotFound", "Friend request does not exist or was cancelled", 404);

		var friendship = await friendshipsService.StartFriendship(friendRequest, cancellationToken);

		return Results.Ok(FriendReadModel.Map(friendRequest.SenderProfile!, friendship.StartedAt));
	}
}