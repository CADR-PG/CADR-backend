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
using Shared.Services;

namespace Community.Core.Features.ChatMessages;

internal record struct PinChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] ChatMessageId MessageId
) : IHttpRequest;

internal sealed class PinChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPut<PinChatMessage, PinChatMessageHandler>("chats/{chatId}/pins/{messageId}")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`Pin.Forbidden`, `Pin.LimitReached`")
		.ProducesError(404, "`Chat.NotFound`, `ChatMessage.NotFound`");
}

internal sealed class PinChatMessageHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider,
	ISnowflakeIdProvider idProvider
) : IHttpRequestHandler<PinChatMessage>
{
	private const int MaxPins = 50;

	public async Task<IResult> Handle(PinChatMessage request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, messageId) = request;

		var participant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Select(chatParticipant => new { chatParticipant.Role })
			.FirstOrDefaultAsync(cancellationToken);

		if (participant is null)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		if (participant.Role != ChatParticipantRole.Admin)
			return new ErrorResult("PinChatMessage.Forbidden", "You cannot pin messages.", 403);

		var message = await dbContext.ChatMessages
			.Where(chatMessage => chatMessage.ChatId == chatId && chatMessage.Id == messageId && chatMessage.DeletedAt == null)
			.Select(chatMessage => new { chatMessage.Type })
			.FirstOrDefaultAsync(cancellationToken);

		if (message is null)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (message.Type == ChatMessageType.Event)
			return new ErrorResult("PinChatMessage.Forbidden", "Event message cannot be pinned!", 400);

		var alreadyPinned = await dbContext.ChatMessagePins
			.AnyAsync(p => p.ChatId == chatId && p.MessageId == messageId, cancellationToken);

		if (alreadyPinned)
			return Results.NoContent();

		var count = await dbContext.ChatMessagePins.CountAsync(chatMessagePin => chatMessagePin.ChatId == chatId, cancellationToken);
		if (count >= MaxPins)
			return new ErrorResult("PinChatMessage.LimitReached", $"A chat can have at most {MaxPins} pins.", 403);

		var pin = ChatMessagePin.Create(chatId, messageId, pinnedById: currentUser.Id, timeProvider);
		var eventMessage = ChatMessage.PinnedEventMessage(pin, idProvider);

		await dbContext.ChatMessagePins.AddAsync(pin, cancellationToken);
		await dbContext.ChatMessages.AddAsync(eventMessage, cancellationToken);

		// signalR send

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}