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

namespace Community.Core.Features.Chats;

internal record struct DeleteGroupChat(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class DeleteGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<DeleteGroupChat, DeleteGroupChatHandler>("group-chats/{chatId:long}/delete")
		.Produces(204)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class DeleteGroupChatHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<DeleteGroupChat>
{
	public async Task<IResult> Handle(
		DeleteGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var chatParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(chatParticipant => chatParticipant.Chat!.Type == ChatType.Group)
			.Include(chatParticipant => chatParticipant.Chat)
			.FirstOrDefaultAsync(cancellationToken);

		if (chatParticipant is null)
			return new ErrorResult("GroupChat.NotFound", "Chat with given ID does not exist", 404);

		if (chatParticipant.Role != ChatParticipantRole.Admin)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation.", 403);

		var chat = chatParticipant.Chat!;
		chat.MarkAsDeleted(currentUser.Id, timeProvider);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}