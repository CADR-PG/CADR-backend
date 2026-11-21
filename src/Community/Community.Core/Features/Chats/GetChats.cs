using Community.Core.Database;
using Community.Core.Database.Extensions;
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
	[FromQuery] int? PageNumber = 1,
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
			.AsNoTracking()
			.AccessibleBy(currentUser.Id);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = LikePattern.Contains(searchPhrase);
			baseDbQuery = baseDbQuery.Where(chat =>
				(chat.Name != null && EF.Functions.ILike(chat.Name, pattern, LikePattern.EscapeCharacter))
				|| chat.Participants.Any(participant => participant.UserId != currentUser.Id
														&& participant.DeactivatedAt == null
														&& ((participant.Name != null && EF.Functions.ILike(participant.Name, pattern, LikePattern.EscapeCharacter))
															|| EF.Functions.ILike(participant.UserProfile!.Name, pattern, LikePattern.EscapeCharacter))));
		}

		var finalDbQuery = baseDbQuery
			.OrderByDescending(chat => chat.Messages.Max(message => (DateTimeOffset?)message.SentAt) ?? chat.CreatedAt)
			.ThenByDescending(chat => chat.Id)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext));

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber, pageSize, cancellationToken);
		return Results.Ok(readModels);
	}
}