using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.Friends;

internal record struct RemoveFriend(
	CurrentUser CurrentUser,
	RemoveFriend.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] UserId? FriendId);
}

internal sealed class RemoveFriendEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RemoveFriend, RemoveFriendHandler>("friends/remove")
		.AddDataAnnotationsValidation<RemoveFriend.Body>()
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`")
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

		await friendshipsService.EndFriendshipIfExists(currentUser.Id, payload.FriendId!.Value, cancellationToken);

		return Results.NoContent();
	}
}
