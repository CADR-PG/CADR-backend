using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
using Community.Core.Models.Chats;
using Community.Core.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Services;

namespace Community.Core.Features.ChatMessages;

internal record struct UnpinChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] ChatMessageId MessageId
) : IHttpRequest;

internal sealed class UnpinChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<UnpinChatMessage, UnpinChatMessageHandler>("chats/{chatId:long}/pinned-messages/{messageId:long}/unpin")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`UnpinChatMessage.Forbidden`, `Chat.ReadOnly`")
		.ProducesError(404, "`Chat.NotFound`");
}

internal sealed class UnpinChatMessageHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<UnpinChatMessage>
{
	public async Task<IResult> Handle(UnpinChatMessage request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, messageId) = request;

		var participant = await dbContext.ChatParticipants
			.AsNoTracking()
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Select(m => new { m.Role, m.Chat!.Type })
			.FirstOrDefaultAsync(cancellationToken);

		if (participant is null)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		if (await dbContext.IsChatReadOnly(chatId, cancellationToken))
			return ChatWriteAccess.ReadOnlyError();

		if (participant.Type != ChatType.Direct && participant.Role != ChatParticipantRole.Admin)
			return new ErrorResult("UnpinChatMessage.Forbidden", "You cannot unpin messages.", 403);

		await dbContext.ChatMessagePins
			.Where(p => p.ChatId == chatId && p.MessageId == messageId)
			.ExecuteDeleteAsync(cancellationToken);

		return Results.NoContent();
	}
}