using Community.Core.Database;
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

internal record struct RenameGroupChat(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	RenameGroupChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body(string Name);
}

internal sealed class RenameGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RenameGroupChat, RenameGroupChatHandler>("group-chats/{chatId:long}/rename")
		.Produces(204)
		.RequireAuthorization()
		.ProducesError(404, "GroupChat.NotFound")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class RenameGroupChatHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<RenameGroupChat>
{
	public async Task<IResult> Handle(
		RenameGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;

		var chat = await dbContext.Chats
			.Where(x => x.Type == ChatType.Group && x.Id == chatId)
			.Include(x => x.Participants.Where(participant => participant.UserId == currentUser.Id))
			.FirstOrDefaultAsync(cancellationToken);

		if (chat is null)
			return new ErrorResult("GroupChat.NotFound", "Chat with given ID does not exist", 404);

		chat.ChangeName(payload.Name);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}