using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Models.Chats;
using Community.Core.ValueObjects;
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

internal record struct GetChatReadStates(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class GetChatReadStatesEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetChatReadStates, GetChatReadStatesHandler>("chats/{chatId}/read-states")
		.Produces<Listed<ChatParticipantReadStateReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetChatReadStatesHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetChatReadStates>
{
	public async Task<IResult> Handle(GetChatReadStates request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var isParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AnyAsync(cancellationToken);

		if (!isParticipant)
			return new ErrorResult("Chat.NotFound", "Chat with given ID does not exists!", 404);

		var baseDbQuery = dbContext.ChatParticipants
			.Where(x => x.ChatId == chatId)
			.AsNoTracking();

		var finalDbQuery = baseDbQuery.Select(ChatParticipantReadStateReadModel.MappingExpression);
		var response = await Listed.Create(finalDbQuery);
		return Results.Ok(response);
	}
}