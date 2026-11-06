using Community.Core.Database;
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

namespace Community.Core.Features.Friends;

internal record struct GetFriends(
	CurrentUser CurrentUser,
	[FromQuery] UserId? FriendId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetFriendsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetFriends, GetFriendsHandler>("friends")
		.Produces<OffsetPaginated<FriendReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetFriendsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetFriends>
{
	public async Task<IResult> Handle(GetFriends request, CancellationToken cancellationToken)
	{
		var (currentUser, friendId, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.Friendships
			.AsNoTracking()
			.Where(friendship => friendship.LowerUserId == currentUser.Id || friendship.HigherUserId == currentUser.Id);

		if (friendId is not null)
		{
			baseDbQuery = baseDbQuery.Where(friendship =>
				friendship.LowerUserId == friendId || friendship.HigherUserId == friendId);
		}

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(friendship => friendship.LowerUserId == currentUser.Id
				? EF.Functions.Like(friendship.HigherUser!.Name, $"%{searchPhrase}%")
				: EF.Functions.Like(friendship.LowerUser!.Name, $"%{searchPhrase}%")
			);
		}

		var finalDbQuery = baseDbQuery.Select(FriendReadModel.GetMappingExpression(currentUser.Id));

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}