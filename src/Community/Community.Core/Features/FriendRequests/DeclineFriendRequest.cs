using Community.Core.Database;
using Community.Core.Database.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;

namespace Community.Core.Features.FriendRequests;

internal record struct DeclineFriendRequest(
	CurrentUser CurrentUser,
	[FromRoute] Guid FriendRequestId
) : IHttpRequest;

internal sealed class DeclineFriendRequestEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<DeclineFriendRequest, DeclineFriendRequestHandler>("friend-requests/{friendRequestId:guid}/decline")
		.Produces(204)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class DeclineFriendRequestHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<DeclineFriendRequest>
{
	public async Task<IResult> Handle(
		DeclineFriendRequest request,
		CancellationToken cancellationToken)
	{
		var (currentUser, friendRequestId) = request;

		var receiverId = currentUser.Id;

		var friendRequest = await dbContext.FriendRequests
			.GetIncomingPendingFriendRequest(friendRequestId, receiverId, cancellationToken);

		if (friendRequest is null)
			return Results.NoContent();

		friendRequest.Decline();
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}