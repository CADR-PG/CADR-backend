using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Friendships;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;

namespace Community.Core.Features.FriendRequests;

internal record struct CancelFriendRequest(
	CurrentUser CurrentUser,
	[FromRoute] Guid FriendRequestId
) : IHttpRequest;

internal sealed class CancelFriendRequestEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CancelFriendRequest, CancelFriendRequestHandler>("friend-requests/{friendRequestId:guid}/cancel")
		.Produces(204)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CancelFriendRequestHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<CancelFriendRequest>
{
	public async Task<IResult> Handle(
		CancelFriendRequest request,
		CancellationToken cancellationToken)
	{
		var (currentUser, friendRequestId) = request;

		var senderId = currentUser.Id;

		var pendingFriendRequest = await dbContext.FriendRequests
			.GetOutgoingPendingFriendRequest(friendRequestId, senderId, cancellationToken);

		if (pendingFriendRequest is null)
			return Results.NoContent();

		pendingFriendRequest.Cancel();
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}