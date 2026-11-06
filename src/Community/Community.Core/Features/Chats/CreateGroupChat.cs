using Community.Core.Database;
using Community.Core.Database.Extensions;
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

internal record struct CreateGroupChat(
	CurrentUser CurrentUser,
	CreateGroupChat.Body Payload
) : IHttpRequest
{
	internal sealed record Body(string Name, List<Guid> MemberIds);
}

internal sealed class CreateGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateGroupChat, CreateGroupChatHandler>("group-chats")
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.InvalidName`, `GroupChat.TooManyMembers`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateGroupChatHandler(
	CommunityDbContext dbContext,
	ISnowflakeIdProvider idProvider,
	TimeProvider timeProvider
) : IHttpRequestHandler<CreateGroupChat>
{
	private const int MaxNameLength = 100;
	private const int MaxMembers = 10;

	public async Task<IResult> Handle(
		CreateGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var name = payload.Name.Trim();
		if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
			return new ErrorResult("GroupChat.InvalidName",
				$"Group name must be between 1 and {MaxNameLength} characters.");

		var requestedMemberIds = (payload.MemberIds ?? [])
			.Select(memberId => new UserId(memberId))
			.Where(memberId => memberId != currentUser.Id)
			.Distinct()
			.ToList();

		if (requestedMemberIds.Count + 1 > MaxMembers)
			return new ErrorResult("GroupChat.TooManyMembers", $"A group can have at most {MaxMembers} members.");

		var finalMemberIds = await dbContext.UserProfiles
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId: null)
			.Where(userProfile => requestedMemberIds.Any(memberId => memberId == userProfile.UserId))
			.Select(x => x.UserId)
			.ToListAsync(cancellationToken);

		var chat = Chat.CreateGroupChat(name, ownerId: currentUser.Id, finalMemberIds, idProvider, timeProvider);

		await dbContext.Chats.AddAsync(chat, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var chatReadModel = await dbContext.Chats
			.Where(x => x.Id == chat.Id)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id))
			.FirstAsync(cancellationToken);

		return Results.Created($"chats/{chat.Id}", chatReadModel);
	}
}