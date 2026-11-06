using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.ValueObjects;

namespace Community.Core.Features.Friends;

internal record struct RemoveFriend(
	CurrentUser CurrentUser,
	RemoveFriend.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid FriendId);
}

internal sealed class RemoveFriendEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RemoveFriend, RemoveFriendHandler>("friends/remove")
		.Produces(204)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class RemoveFriendHandler(
	FriendshipsService friendshipsService
) : IHttpRequestHandler<RemoveFriend>
{
	public async Task<IResult> Handle(
		RemoveFriend request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;
		var friendId = new UserId(payload.FriendId);

		await friendshipsService.EndFriendshipIfExists(currentUser.Id, friendId, cancellationToken);

		return Results.NoContent();
	}
}