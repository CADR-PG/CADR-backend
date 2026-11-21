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

namespace Community.Core.Features.FriendRequests;

internal record struct GetOutgoingFriendRequests(
	CurrentUser CurrentUser,
	[FromQuery] UserId? ReceiverId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 1,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetOutgoingFriendRequestsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetOutgoingFriendRequests, GetOutgoingFriendRequestsHandler>("friend-requests/outgoing")
		.Produces<OffsetPaginated<OutgoingFriendRequestReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetOutgoingFriendRequestsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetOutgoingFriendRequests>
{
	public async Task<IResult> Handle(GetOutgoingFriendRequests request, CancellationToken cancellationToken)
	{
		var (currentUser, receiverId, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.FriendRequests
			.AsNoTracking()
			.Where(x => x.SenderId == currentUser.Id && x.Status == FriendRequestStatus.Pending);

		if (receiverId is not null)
		{
			baseDbQuery = baseDbQuery.Where(x => x.ReceiverId == receiverId);
		}

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = LikePattern.Contains(searchPhrase);
			baseDbQuery = baseDbQuery.Where(friendRequest => EF.Functions.ILike(friendRequest.ReceiverProfile!.Name, pattern, LikePattern.EscapeCharacter));
		}

		var finalDbQuery = baseDbQuery
			.OrderByDescending(friendRequest => friendRequest.SentAt)
			.Select(OutgoingFriendRequestReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber, pageSize, cancellationToken);
		return Results.Ok(readModels);
	}
}