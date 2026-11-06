using Community.Core.Database;
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
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetBlockedUsersEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetBlockedUsers, GetBlockedUsersHandler>("blocked-user")
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
			baseDbQuery = baseDbQuery.Where(userBlock => userBlock.BlockerId == blockedId);
		}

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(userBlock => EF.Functions.Like(userBlock.BlockedProfile!.Name, $"%{searchPhrase}%"));
		}

		var finalDbQuery = baseDbQuery.Select(BlockedUserReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}