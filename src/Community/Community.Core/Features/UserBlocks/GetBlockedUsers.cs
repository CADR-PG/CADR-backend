using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Friendships;
using Community.Core.Models.Friendships;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Core.Features.UserBlocks;

internal record struct GetBlockedUsers(
	CurrentUser CurrentUser,
	[FromQuery] UserId? BlockedId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 1,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetBlockedUsersEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetBlockedUsers, GetBlockedUsersHandler>("blocked-users")
		.Produces<OffsetPaginated<BlockedUserReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetBlockedUsersHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetBlockedUsers>
{
	public async Task<IResult> Handle(GetBlockedUsers request, CancellationToken cancellationToken)
	{
		var (currentUser, blockedId, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.UserBlocks
			.AsNoTracking()
			.Where(userBlock => userBlock.BlockerId == currentUser.Id);

		if (blockedId is not null)
		{
			baseDbQuery = baseDbQuery.Where(userBlock => userBlock.BlockedId == blockedId);
		}

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = LikePattern.Contains(searchPhrase);
			baseDbQuery = baseDbQuery.Where(userBlock => EF.Functions.ILike(userBlock.BlockedProfile!.Name, pattern, LikePattern.EscapeCharacter));
		}

		var finalDbQuery = baseDbQuery
			.OrderByDescending(userBlock => userBlock.BlockedAt)
			.Select(BlockedUserReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber, pageSize, cancellationToken);
		return Results.Ok(readModels);
	}
}