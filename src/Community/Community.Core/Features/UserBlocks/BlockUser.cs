using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.UserBlocks;
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
using Shared.ValueObjects;

namespace Community.Core.Features.UserBlocks;

internal record struct BlockUser(
	CurrentUser CurrentUser,
	[FromBody] BlockUser.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid UserId);
}

internal sealed class BlockUserEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<BlockUser, BlockUserHandler>("blocked-users/block")
		.Produces<BlockedUserReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class BlockUserHandler(
	CommunityDbContext dbContext,
	FriendshipsService friendshipsService
) : IHttpRequestHandler<BlockUser>
{
	public async Task<IResult> Handle(
		BlockUser request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var blockedUserId = new UserId(payload.UserId);

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		var userBlock = await dbContext.UserBlocks
			.FirstOrDefaultAsync(userBlock => userBlock.BlockerId == currentUser.Id && userBlock.BlockedId == blockedUserId, cancellationToken);

		if (userBlock is not null)
			return Results.Ok(BlockedUserReadModel.Map(userBlock));

		userBlock = new UserBlock {
			BlockerId = currentUser.Id,
			BlockedId = blockedUserId,
			BlockedAt = DateTimeOffset.UtcNow
		};

		await dbContext.SaveChangesAsync(cancellationToken);
		await friendshipsService.EndFriendshipIfExists(currentUser.Id, blockedUserId, cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return Results.Ok(BlockedUserReadModel.Map(userBlock));
	}
}