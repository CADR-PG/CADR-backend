using Community.Core.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.ValueObjects;

namespace Community.Core.Features.UserBlocks;

internal record struct UnblockUser(
	CurrentUser CurrentUser,
	[FromBody] UnblockUser.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid UserId);
}

internal sealed class UnblockUserEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<UnblockUser, UnblockUserHandler>("blocked-users/unblock")
		.Produces(204)
		.RequireAuthorization()
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
		var blockedUserId = new UserId(payload.UserId);

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		await dbContext.UserBlocks
			.Where(userBlock => userBlock.BlockerId == currentUser.Id && userBlock.BlockedId == blockedUserId)
			.ExecuteDeleteAsync(cancellationToken);

		return Results.NoContent();
	}
}