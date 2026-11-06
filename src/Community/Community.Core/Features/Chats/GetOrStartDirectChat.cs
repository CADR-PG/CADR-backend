using Community.Core.Database;
using Community.Core.Entities.Chats;
using Community.Core.Models.Chats;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Core.Features.Chats;

internal record struct GetOrStartDirectChat(
	CurrentUser CurrentUser,
	GetOrStartDirectChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body(Guid UserId);
}

internal sealed class GetOrStartDirectChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<GetOrStartDirectChat, GetOrStartDirectChatHandler>("direct-chats")
		.Produces<ChatReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`DirectChat.NotFriends`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`FriendRequestNotFound`");
}

internal sealed class GetOrStartDirectChatHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider,
	ISnowflakeIdProvider idProvider
) : IHttpRequestHandler<GetOrStartDirectChat>
{
	public async Task<IResult> Handle(
		GetOrStartDirectChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var currentChatterId = currentUser.Id;
		var secondChatterId = new UserId(payload.UserId);

		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(currentChatterId, secondChatterId);

		var chatReadModel = await dbContext.Chats
			.Where(x => x.DirectChatLowerUserId == lowerUserId && x.DirectChatHigherUserId == higherUserId)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id))
			.FirstOrDefaultAsync(cancellationToken);

		if (chatReadModel is not null) return Results.Ok(chatReadModel);

		var friendshipExists = await dbContext.Friendships
			.AnyAsync(friendship =>
				friendship.LowerUserId == lowerUserId && friendship.HigherUserId == higherUserId, cancellationToken);
		if (!friendshipExists) return new ErrorResult("DirectChat.NotFriends", "Direct chat can only be created between users who are friends.");

		var chat = Chat.CreateDirectChat(lowerUserId, higherUserId, idProvider, timeProvider);

		await dbContext.Chats.AddAsync(chat, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		chatReadModel = await dbContext.Chats
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id))
			.FirstAsync(cancellationToken);

		return Results.Created($"chats/{chat.Id}",chatReadModel);
	}
}