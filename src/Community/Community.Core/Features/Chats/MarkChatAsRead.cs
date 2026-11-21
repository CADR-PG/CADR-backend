using Community.Core.Database;
using Community.Core.Database.Extensions;
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

namespace Community.Core.Features.Chats;

internal record struct MarkChatAsRead(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	MarkChatAsRead.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] ChatMessageId? MessageId);
}

internal sealed class MarkChatAsReadEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<MarkChatAsRead, MarkChatAsReadHandler>("chats/{chatId:long}/read")
		.AddDataAnnotationsValidation<MarkChatAsRead.Body>()
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`Chat.NotFound`, `ChatMessage.NotFound`");
}

internal sealed class MarkChatAsReadHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<MarkChatAsRead>
{
	public async Task<IResult> Handle(MarkChatAsRead request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;
		var messageId = payload.MessageId!.Value;

		var participant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.FirstOrDefaultAsync(cancellationToken);

		if (participant is null)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		var messageExists = await dbContext.ChatMessages
			.AnyAsync(message => message.ChatId == chatId && message.Id == messageId, cancellationToken);

		if (!messageExists)
			return new ErrorResult("ChatMessage.NotFound", "Chat message not found.", 404);

		if (participant.MarkAsRead(messageId, timeProvider))
			await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}