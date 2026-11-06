using Community.Core.Database;
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
		.MapGet<GetChatParticipants, GetChatParticipantsHandler>("chats/{chatId}/members")
		.Produces<Listed<ChatParticipantReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetChatParticipantsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetChatParticipants>
{
	public async Task<IResult> Handle(GetChatParticipants request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var isParticipant = await dbContext.ChatParticipants.AnyAsync(chatParticipant => chatParticipant.UserId == currentUser.Id && chatParticipant.ChatId == chatId, cancellationToken);
		if (!isParticipant)
			return new ErrorResult("GroupChat.NotFound", "Group chat with given ID does not exists!", 404);

		var baseDbQuery = dbContext.ChatParticipants
			.Where(x => x.ChatId == chatId)
			.AsNoTracking();

		var finalDbQuery = baseDbQuery.Select(ChatParticipantReadModel.MappingExpression);
		var response = await Listed.Create(finalDbQuery);
		return Results.Ok(response);
	}
}