using Community.Core.Database;
using Community.Core.Models.Chats;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Services;

namespace Community.Core.Features.Chats;

internal record struct GetPaginatedChats(
	CurrentUser CurrentUser,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetPaginatedChatsEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetPaginatedChats, GetPaginatedChatsHandler>("chats")
		.Produces<OffsetPaginated<ChatReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetPaginatedChatsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetPaginatedChats>
{
	public async Task<IResult> Handle(GetPaginatedChats request, CancellationToken cancellationToken)
	{
		var (currentUser, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.Chats
			.Where(chat => chat.Participants.Any(participant => participant.UserId == currentUser.Id))
			.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(chat => EF.Functions.Like(chat.Name, $"%{searchPhrase}%")
			                                        || chat.Participants.Where(x => x.UserId != currentUser.Id).Any(participant =>
				                                        EF.Functions.Like(participant.Name, $"%{searchPhrase}%")));
		}

		var finalDbQuery = baseDbQuery.Select(ChatReadModel.GetMappingExpression(currentUser.Id));

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}