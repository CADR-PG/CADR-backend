using Community.Core.Database;
using Community.Core.Database.Configurations;
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

internal record struct CreateGroupChat(
	CurrentUser CurrentUser,
	CreateGroupChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body(
		[property: Required] [property: MaxLength(ChatConfiguration.MaxNameLength)] string Name,
		[property: MaxLength(CreateGroupChatHandler.MaxMembers)] List<UserId>? MemberIds
	);
}

internal sealed class CreateGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateGroupChat, CreateGroupChatHandler>("group-chats")
		.AddDataAnnotationsValidation<CreateGroupChat.Body>()
		.Produces<ChatReadModel>(StatusCodes.Status201Created)
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `GroupChat.InvalidName`, `GroupChat.TooManyMembers`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateGroupChatHandler(
	CommunityDbContext dbContext,
	ISnowflakeIdProvider idProvider,
	TimeProvider timeProvider,
	UserProfilesSynchronizer userProfilesSynchronizer
) : IHttpRequestHandler<CreateGroupChat>
{
	public const int MaxMembers = 10;

	public async Task<IResult> Handle(
		CreateGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var name = payload.Name.Trim();
		if (name.Length == 0)
			return new ErrorResult("GroupChat.InvalidName",
				$"Group name must be between 1 and {ChatConfiguration.MaxNameLength} characters.");

		var requestedMemberIds = (payload.MemberIds ?? [])
			.Where(memberId => memberId != currentUser.Id)
			.Distinct()
			.ToList();

		if (requestedMemberIds.Count + 1 > MaxMembers)
			return new ErrorResult("GroupChat.TooManyMembers", $"A group can have at most {MaxMembers} members.");

		await userProfilesSynchronizer.EnsureSynchronized(requestedMemberIds, cancellationToken);

		var finalMemberIds = await dbContext.UserProfiles
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId: null)
			.Where(userProfile => requestedMemberIds.Contains(userProfile.UserId))
			.Select(userProfile => userProfile.UserId)
			.ToListAsync(cancellationToken);

		var chat = Chat.CreateGroupChat(name, ownerId: currentUser.Id, finalMemberIds, idProvider, timeProvider);

		await dbContext.Chats.AddAsync(chat, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var chatReadModel = await dbContext.Chats
			.AsNoTracking()
			.Where(x => x.Id == chat.Id)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext))
			.FirstAsync(cancellationToken);

		return Results.Created($"chats/{chat.Id}", chatReadModel);
	}
}
