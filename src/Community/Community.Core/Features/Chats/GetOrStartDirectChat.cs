using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
using Community.Core.Models.Chats;
using Community.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.Services;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.Chats;

internal record struct GetOrStartDirectChat(
	CurrentUser CurrentUser,
	GetOrStartDirectChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required] UserId? UserId);
}

internal sealed class GetOrStartDirectChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<GetOrStartDirectChat, GetOrStartDirectChatHandler>("direct-chats")
		.AddDataAnnotationsValidation<GetOrStartDirectChat.Body>()
		.Produces<ChatReadModel>()
		.Produces<ChatReadModel>(StatusCodes.Status201Created)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `DirectChat.InvalidUser`, `DirectChat.NotFriends`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetOrStartDirectChatHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider,
	ISnowflakeIdProvider idProvider,
	UserProfilesSynchronizer userProfilesSynchronizer
) : IHttpRequestHandler<GetOrStartDirectChat>
{
	public async Task<IResult> Handle(
		GetOrStartDirectChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var currentChatterId = currentUser.Id;
		var secondChatterId = payload.UserId!.Value;

		if (secondChatterId == currentChatterId)
			return new ErrorResult("DirectChat.InvalidUser", "You cannot start a direct chat with yourself.");

		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(currentChatterId, secondChatterId);

		var chatReadModel = await dbContext.Chats
			.AsNoTracking()
			.Where(chat => chat.Type == ChatType.Direct
			               && chat.DirectChatLowerUserId == lowerUserId
			               && chat.DirectChatHigherUserId == higherUserId)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext))
			.FirstOrDefaultAsync(cancellationToken);

		if (chatReadModel is not null) return Results.Ok(chatReadModel);

		var isBlocked = await dbContext.UserBlocks.Between(lowerUserId, higherUserId).AnyAsync(cancellationToken);

		var friendshipExists = await dbContext.Friendships
			.AnyAsync(friendship => friendship.LowerUserId == lowerUserId && friendship.HigherUserId == higherUserId, cancellationToken);

		if (isBlocked || !friendshipExists)
			return new ErrorResult("DirectChat.NotFriends", "Direct chat can only be created between users who are friends.");

		await userProfilesSynchronizer.EnsureSynchronized(secondChatterId, cancellationToken);

		var chat = Chat.CreateDirectChat(lowerUserId, higherUserId, idProvider, timeProvider);

		await dbContext.Chats.AddAsync(chat, cancellationToken);

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();

			chatReadModel = await dbContext.Chats
				.AsNoTracking()
				.Where(x => x.Type == ChatType.Direct
				            && x.DirectChatLowerUserId == lowerUserId
				            && x.DirectChatHigherUserId == higherUserId)
				.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext))
				.FirstAsync(cancellationToken);

			return Results.Ok(chatReadModel);
		}

		chatReadModel = await dbContext.Chats
			.AsNoTracking()
			.Where(x => x.Id == chat.Id)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext))
			.FirstAsync(cancellationToken);

		return Results.Created($"chats/{chat.Id}", chatReadModel);
	}
}
