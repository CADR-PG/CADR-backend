using Community.Core.Database;
using Community.Core.Database.Configurations;
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
using Shared.Endpoints.Validation;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.Chats;

internal record struct RenameGroupChat(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	RenameGroupChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] [property: MaxLength(ChatConfiguration.MaxNameLength)] string Name);
}

internal sealed class RenameGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RenameGroupChat, RenameGroupChatHandler>("group-chats/{chatId:long}/rename")
		.AddDataAnnotationsValidation<RenameGroupChat.Body>()
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `GroupChat.InvalidName`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GroupChat.NotFound`");
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

		var name = payload.Name.Trim();
		if (name.Length == 0)
			return new ErrorResult("GroupChat.InvalidName",
				$"Group name must be between 1 and {ChatConfiguration.MaxNameLength} characters.");

		var chat = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(participant => participant.Chat!.Type == ChatType.Group)
			.Select(participant => participant.Chat!)
			.FirstOrDefaultAsync(cancellationToken);

		if (chat is null)
			return new ErrorResult("GroupChat.NotFound", "Chat with given ID does not exist.", 404);

		chat.ChangeName(name);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}
