using Community.Core.Database;
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
using Shared.Services;

namespace Community.Core.Features.ChatMessages;

internal record struct SendChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	SendChatMessage.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid ClientMessageId, string Content, ChatMessageId? RepliedMessageId);
}

internal sealed class SendChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<SendChatMessage, SendChatMessageHandler>("chats/{chatId:long}/send-message")
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.InvalidName`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class SendChatMessageHandler(
	CommunityDbContext dbContext,
	ISnowflakeIdProvider idProvider,
	TimeProvider timeProvider
) : IHttpRequestHandler<SendChatMessage>
{
	private const int MaxContentLength = 4000;

	public async Task<IResult> Handle(
		SendChatMessage request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;

		var content = payload.Content.Trim();
		if (string.IsNullOrEmpty(content))
			return new ErrorResult("Message.EmptyContent", "Message content cannot be empty.");
		if (content.Length > MaxContentLength)
			return new ErrorResult("Message.TooLong", $"Message cannot exceed {MaxContentLength} characters.");
		if (payload.ClientMessageId == Guid.Empty)
			return new ErrorResult("Message.InvalidClientMessageId", "ClientMessageId is required.");

		var existing = await dbContext.ChatMessages
			.Where(chatMessage => chatMessage.ChatId == chatId
			            && chatMessage.SenderId == currentUser.Id
			            && chatMessage.ClientMessageId == payload.ClientMessageId)
			.Select(ChatMessageReadModel.MappingExpression)
			.FirstOrDefaultAsync(cancellationToken);

		if (existing is not null) return Results.Ok(existing);

		var chatExists = await dbContext.Chats.AnyAsync(c => c.Id == chatId, cancellationToken);
		if (!chatExists)
			return new ErrorResult("Chat.NotFound", "Chat not found.");

		var isParticipant = await dbContext.ChatParticipants.AnyAsync(chatParticipant => chatParticipant.ChatId == chatId && chatParticipant.UserId == currentUser.Id, cancellationToken);
		if (!isParticipant)
			return new ErrorResult("Chat.NotMember", "You are not a member of this chat.");

		if (payload.RepliedMessageId is { } replyId)
		{
			var messageToReplyExists = await dbContext.ChatMessages.AnyAsync(
				m => m.Id == replyId && m.ChatId == chatId, cancellationToken);
			if (!messageToReplyExists)
				return new ErrorResult("Message.InvalidReply", "Replied message does not exist in this chat.");
		}

		var message = ChatMessage.UserMessage(chatId, currentUser.Id, payload.ClientMessageId, payload.Content, payload.RepliedMessageId, idProvider, timeProvider);

		dbContext.ChatMessages.Add(message);

		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = ChatMessageReadModel.Map(message);
		return Results.Created($"chats/{chatId}/messages/{message.Id}", readModel);
	}
}