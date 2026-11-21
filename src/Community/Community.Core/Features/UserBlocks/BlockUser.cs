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
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.UserBlocks;

internal record struct BlockUser(
	CurrentUser CurrentUser,
	[FromBody] BlockUser.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] UserId? UserId);
}

internal sealed class BlockUserEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<BlockUser, BlockUserHandler>("blocked-users/block")
		.AddDataAnnotationsValidation<BlockUser.Body>()
		.Produces<BlockedUserReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `UserBlock.InvalidUser`, `ProfileNotFound`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class BlockUserHandler(
	CommunityDbContext dbContext,
	FriendshipsService friendshipsService,
	UserProfilesSynchronizer userProfilesSynchronizer,
	TimeProvider timeProvider
) : IHttpRequestHandler<BlockUser>
{
	public async Task<IResult> Handle(
		BlockUser request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;
		var blockedUserId = payload.UserId!.Value;

		if (blockedUserId == currentUser.Id)
			return new ErrorResult("UserBlock.InvalidUser", "You cannot block yourself.");

		await userProfilesSynchronizer.EnsureSynchronized(blockedUserId, cancellationToken);

		var profileExists = await dbContext.UserProfiles.AnyAsync(profile => profile.UserId == blockedUserId, cancellationToken);
		if (!profileExists)
			return new ErrorResult("ProfileNotFound", "No public profile was found for the specified user ID.");

		var existingReadModel = await dbContext.UserBlocks
			.AsNoTracking()
			.Where(userBlock => userBlock.BlockerId == currentUser.Id && userBlock.BlockedId == blockedUserId)
			.Select(BlockedUserReadModel.MappingExpression)
			.FirstOrDefaultAsync(cancellationToken);

		if (existingReadModel is not null)
			return Results.Ok(existingReadModel);

		try
		{
			await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

			var userBlock = new UserBlock
			{
				BlockerId = currentUser.Id,
				BlockedId = blockedUserId,
				BlockedAt = timeProvider.GetUtcNow()
			};

			dbContext.UserBlocks.Add(userBlock);
			await dbContext.SaveChangesAsync(cancellationToken);

			await friendshipsService.EndFriendshipIfExists(currentUser.Id, blockedUserId, cancellationToken);
			await friendshipsService.ClosePendingFriendRequestsOnBlock(currentUser.Id, blockedUserId, cancellationToken);

			await transaction.CommitAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();
		}

		var readModel = await dbContext.UserBlocks
			.AsNoTracking()
			.Where(x => x.BlockerId == currentUser.Id && x.BlockedId == blockedUserId)
			.Select(BlockedUserReadModel.MappingExpression)
			.FirstAsync(cancellationToken);

		return Results.Ok(readModel);
	}
}