using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;

namespace Community.Core.Features.ChatMessages;

internal record struct DeleteChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] ChatMessageId MessageId
) : IHttpRequest;

internal sealed class DeleteChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapDelete<DeleteChatMessage, DeleteChatMessageHandler>("chats/{chatId:long}/messages/{messageId:long}")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`ChatMessage.Invalid`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`ChatMessage.Forbidden`, `Chat.ReadOnly`")
		.ProducesError(404, "`ChatMessage.NotFound`");
}

internal sealed class DeleteChatMessageHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<DeleteChatMessage>
{
	public async Task<IResult> Handle(DeleteChatMessage request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, messageId) = request;

		var participant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken);

		if (participant is null)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (await dbContext.IsChatReadOnly(chatId, cancellationToken))
			return ChatWriteAccess.ReadOnlyError();

		var message = await dbContext.ChatMessages
			.FirstOrDefaultAsync(m => m.ChatId == chatId && m.Id == messageId, cancellationToken);

		if (message is null)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		var isAuthor = message.SenderId == currentUser.Id;
		var isAdmin = participant.Role == ChatParticipantRole.Admin;

		if (!isAuthor && !isAdmin)
			return new ErrorResult("ChatMessage.Forbidden", "You cannot delete this message.", 403);

		if (message.Type is ChatMessageType.Event)
			return new ErrorResult("ChatMessage.Invalid", "You cannot delete this message.", 400);

		if (message.DeletedAt is not null)
			return Results.NoContent();

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		message.MarkAsDeleted(currentUser.Id, timeProvider);

		await dbContext.ChatMessagePins
			.Where(p => p.ChatId == chatId && p.MessageId == messageId)
			.ExecuteDeleteAsync(cancellationToken);

		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Results.NoContent();
	}
}