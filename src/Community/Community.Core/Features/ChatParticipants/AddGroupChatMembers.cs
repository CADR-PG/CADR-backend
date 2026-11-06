using Community.Core.Database;
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
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Core.Features.ChatParticipants;

internal record struct AddGroupChatMembers(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	AddGroupChatMembers.Body Payload
) : IHttpRequest
{
	internal sealed record Body(string Name, List<UserId> MemberIds);
}

internal sealed class AddGroupChatMembersEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddGroupChatMembers, AddGroupChatMembersHandler>("group-chats/{chatId:long}/members")
		.Produces<Listed<ChatParticipantReadModel>>()
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.NotFound`, `GroupChat.TooManyMembers`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class AddGroupChatMembersHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<AddGroupChatMembers>
{
	private const int MaxMembers = 10;

	public async Task<IResult> Handle(
		AddGroupChatMembers request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;

		var currentMemberRole = await dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId
			            && m.UserId == currentUser.Id
			            && m.Chat!.Type == ChatType.Group)
			.Select(m => (ChatParticipantRole?)m.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (currentMemberRole is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat not found.", 404);

		if (currentMemberRole != ChatParticipantRole.Admin)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation", 403);

		var requestedMemberIds = payload.MemberIds
			.Select(memberId => new UserId(memberId))
			.Where(memberId => memberId != currentUser.Id)
			.Distinct()
			.ToList();

		var chatMemberCount = await dbContext.ChatParticipants.CountAsync(chatParticipant => chatParticipant.ChatId == chatId, cancellationToken);
		if (chatMemberCount + requestedMemberIds.Count > MaxMembers)
			return new ErrorResult("GroupChat.TooManyMembers", $"A group can have at most {MaxMembers} members.");

		var finalMemberIds = await dbContext.UserProfiles
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId: chatId)
			.Where(userProfile => requestedMemberIds.Any(memberId => memberId == userProfile.UserId))
			.Select(x => x.UserId)
			.ToListAsync(cancellationToken);

		var joinedAt = DateTimeOffset.UtcNow;
		var chatMembers = finalMemberIds
			.Select(memberId => ChatParticipant.CreateParticipant(chatId, memberId, joinedAt))
			.ToList();

		await dbContext.ChatParticipants.AddRangeAsync(chatMembers, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var allChatMembersQuery = dbContext.ChatParticipants
			.Where(x => x.ChatId == chatId)
			.Select(ChatParticipantReadModel.MappingExpression);

		var response = await Listed.Create(allChatMembersQuery);
		return Results.Ok(response);
	}
}