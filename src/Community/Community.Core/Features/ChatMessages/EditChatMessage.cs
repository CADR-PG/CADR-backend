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
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.ChatMessages;

internal record struct EditChatMessage(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] ChatMessageId MessageId,
	EditChatMessage.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] [property: MaxLength(ChatMessageConfiguration.MaxContentLength)] string Content);
}

internal sealed class EditChatMessageEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<EditChatMessage, EditChatMessageHandler>("chats/{chatId:long}/messages/{messageId:long}/edit")
		.AddDataAnnotationsValidation<EditChatMessage.Body>()
		.Produces<ChatMessageReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `Message.EmptyContent`, `ChatMessage.Invalid`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`Chat.ReadOnly`")
		.ProducesError(404, "`ChatMessage.NotFound`");
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
		if (content.Length == 0)
			return new ErrorResult("Message.EmptyContent", "Message content cannot be empty.");

		var isActiveParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AnyAsync(cancellationToken);

		if (!isActiveParticipant)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (await dbContext.IsChatReadOnly(chatId, cancellationToken))
			return ChatWriteAccess.ReadOnlyError();

		var message = await dbContext.ChatMessages
			.Where(chatMessage => chatMessage.ChatId == chatId
			                      && chatMessage.SenderId == currentUser.Id
			                      && chatMessage.Id == messageId
			                      && chatMessage.DeletedAt == null)
			.FirstOrDefaultAsync(cancellationToken);

		if (message is null)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (message.Type is not (ChatMessageType.User or ChatMessageType.Reply))
			return new ErrorResult("ChatMessage.Invalid", "You cannot edit this message.");

		message.Edit(content, timeProvider);
		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = await dbContext.ChatMessages
			.AsNoTracking()
			.Where(chatMessage => chatMessage.Id == message.Id)
			.Select(ChatMessageReadModel.MappingExpression)
			.FirstAsync(cancellationToken);

		return Results.Ok(readModel);
	}
}
