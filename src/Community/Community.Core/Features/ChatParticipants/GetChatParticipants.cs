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

namespace Community.Core.Features.ChatParticipants;

internal record struct GetChatParticipants(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class GetChatParticipantsEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetChatParticipants, GetChatParticipantsHandler>("chats/{chatId:long}/members")
		.Produces<Listed<ChatParticipantReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`Chat.NotFound`");
}

internal sealed class GetChatParticipantsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetChatParticipants>
{
	public async Task<IResult> Handle(GetChatParticipants request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var isParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AnyAsync(cancellationToken);

		if (!isParticipant)
			return new ErrorResult("Chat.NotFound", "Chat with given ID does not exist.", 404);

		var finalDbQuery = dbContext.ChatParticipants
			.AsNoTracking()
			.Active(chatId)
			.OrderBy(participant => participant.JoinedAt)
			.Select(ChatParticipantReadModel.MappingExpression);

		var response = await Listed.Create(finalDbQuery, cancellationToken);
		return Results.Ok(response);
	}
}