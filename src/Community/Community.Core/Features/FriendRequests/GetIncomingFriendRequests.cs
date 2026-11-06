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

namespace Community.Core.Features.FriendRequests;

internal record struct GetIncomingFriendRequests(
	CurrentUser CurrentUser,
	[FromQuery] UserId? SenderId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetIncomingFriendRequestsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetIncomingFriendRequests, GetIncomingFriendRequestsHandler>("friend-requests/incoming")
		.Produces<OffsetPaginated<OutgoingFriendRequestReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetIncomingFriendRequestsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetIncomingFriendRequests>
{
	public async Task<IResult> Handle(GetIncomingFriendRequests request, CancellationToken cancellationToken)
	{
		var (currentUser, senderId, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.FriendRequests
			.AsNoTracking()
			.Where(x => x.ReceiverId == currentUser.Id && x.Status == FriendRequestStatus.Pending);

		if (senderId is not null)
		{
			baseDbQuery = baseDbQuery.Where(x => x.SenderId == senderId);
		}

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(friendRequest => EF.Functions.Like(friendRequest.SenderProfile!.Name, $"%{searchPhrase}%"));
		}

		var finalDbQuery = baseDbQuery.Select(IncomingFriendRequestReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}