using Community.Core.Database;
using Community.Core.Database.Configurations;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
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
using Shared.Endpoints.Validation;
using Shared.Services;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.ChatMessages;

internal record struct SendChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	SendChatMessage.Body Payload
) : IHttpRequest
{
	internal sealed record Body(
		Guid ClientMessageId,
		[property: Required][property: MaxLength(ChatMessageConfiguration.MaxContentLength)] string Content,
		ChatMessageId? RepliedMessageId
	);
}

internal sealed class SendChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<SendChatMessage, SendChatMessageHandler>("chats/{chatId:long}/send-message")
		.AddDataAnnotationsValidation<SendChatMessage.Body>()
		.Produces<ChatMessageReadModel>(StatusCodes.Status201Created)
		.Produces<ChatMessageReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `Message.EmptyContent`, `Message.InvalidClientMessageId`, `Message.InvalidReply`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`Chat.ReadOnly`")
		.ProducesError(404, "`Chat.NotFound`");
}

internal sealed class SendChatMessageHandler(
	CommunityDbContext dbContext,
	ISnowflakeIdProvider idProvider,
	TimeProvider timeProvider
) : IHttpRequestHandler<SendChatMessage>
{
	public async Task<IResult> Handle(
		SendChatMessage request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;

		var content = payload.Content.Trim();
		if (content.Length == 0)
			return new ErrorResult("Message.EmptyContent", "Message content cannot be empty.");
		if (payload.ClientMessageId == Guid.Empty)
			return new ErrorResult("Message.InvalidClientMessageId", "ClientMessageId is required.");

		var participant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.FirstOrDefaultAsync(cancellationToken);

		if (participant is null)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		var existing = await FindByClientMessageId(chatId, currentUser.Id, payload.ClientMessageId, cancellationToken);
		if (existing is not null) return Results.Ok(existing);

		if (await dbContext.IsChatReadOnly(chatId, cancellationToken))
			return ChatWriteAccess.ReadOnlyError();

		if (payload.RepliedMessageId is { } replyId)
		{
			var messageToReplyExists = await dbContext.ChatMessages.AnyAsync(
				chatMessage => chatMessage.Id == replyId
							   && chatMessage.ChatId == chatId
							   && chatMessage.DeletedAt == null
							   && chatMessage.Type != ChatMessageType.Event,
				cancellationToken);

			if (!messageToReplyExists)
				return new ErrorResult("Message.InvalidReply", "Replied message does not exist in this chat.");
		}

		var message = ChatMessage.UserMessage(chatId, currentUser.Id, payload.ClientMessageId, content, payload.RepliedMessageId, idProvider, timeProvider);

		dbContext.ChatMessages.Add(message);
		participant.MarkAsRead(message.Id, timeProvider);

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();
			existing = await FindByClientMessageId(chatId, currentUser.Id, payload.ClientMessageId, cancellationToken);
			return Results.Ok(existing);
		}

		var readModel = await dbContext.ChatMessages
			.AsNoTracking()
			.Where(chatMessage => chatMessage.Id == message.Id)
			.Select(ChatMessageReadModel.MappingExpression)
			.FirstAsync(cancellationToken);

		return Results.Created($"chats/{chatId}/messages/{message.Id}", readModel);
	}

	private Task<ChatMessageReadModel?> FindByClientMessageId(ChatId chatId, UserId senderId, Guid clientMessageId, CancellationToken cancellationToken)
		=> dbContext.ChatMessages
			.AsNoTracking()
			.Where(chatMessage => chatMessage.ChatId == chatId
								  && chatMessage.SenderId == senderId
								  && chatMessage.ClientMessageId == clientMessageId)
			.Select(ChatMessageReadModel.MappingExpression)
			.FirstOrDefaultAsync(cancellationToken);
}