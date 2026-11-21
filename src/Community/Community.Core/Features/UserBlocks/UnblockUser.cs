using Community.Core.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.UserBlocks;

internal record struct UnblockUser(
	CurrentUser CurrentUser,
	[FromBody] UnblockUser.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] UserId? UserId);
}

internal sealed class UnblockUserEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<UnblockUser, UnblockUserHandler>("blocked-users/unblock")
		.AddDataAnnotationsValidation<UnblockUser.Body>()
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class UnblockUserHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<UnblockUser>
{
	public async Task<IResult> Handle(
		UnblockUser request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;
		var blockedUserId = payload.UserId!.Value;

		await dbContext.UserBlocks
			.Where(userBlock => userBlock.BlockerId == currentUser.Id && userBlock.BlockedId == blockedUserId)
			.ExecuteDeleteAsync(cancellationToken);

		return Results.NoContent();
	}
}