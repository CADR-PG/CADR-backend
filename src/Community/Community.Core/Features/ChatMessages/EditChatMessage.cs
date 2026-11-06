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

namespace Community.Core.Features.ChatMessages;

internal record struct EditChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] ChatMessageId MessageId,
	EditChatMessage.Body Payload
) : IHttpRequest
{
	internal sealed record Body(string Content);
}

internal sealed class EditChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<EditChatMessage, EditChatMessageHandler>("chats/{chatId:long}/messages/{messageId:long}/edit")
		.RequireAuthorization()
		.ProducesError(400, "ChatMessage.NotFound")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class EditChatMessageHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<EditChatMessage>
{
	public async Task<IResult> Handle(
		EditChatMessage request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, messageId, payload) = request;

		var content = payload.Content.Trim();

		var message = await dbContext.ChatMessages
			.Where(chatMessage => chatMessage.ChatId == chatId
			            && chatMessage.SenderId == currentUser.Id
			            && chatMessage.Id == messageId)
			.FirstOrDefaultAsync(cancellationToken);

		if (message is null)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (message.Type == ChatMessageType.Event)
			return new ErrorResult("ChatMessage.Invalid", "You cannot edit this message.", 400);

		message.Edit(content, timeProvider);

		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = ChatMessageReadModel.Map(message);
		return Results.Ok(readModel);
	}
}