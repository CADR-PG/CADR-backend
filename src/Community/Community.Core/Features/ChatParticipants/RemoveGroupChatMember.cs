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
using Shared.ValueObjects;

namespace Community.Core.Features.ChatParticipants;

internal record struct RemoveGroupChatMember(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] Guid MemberId
) : IHttpRequest;

internal sealed class RemoveGroupChatMemberEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RemoveGroupChatMember, RemoveGroupChatMemberHandler>(
			"group-chats/{chatId}/members/{memberId}")
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.CannotRemoveSelf`, `GroupChat.CannotRemoveAdmin`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class RemoveGroupChatMemberHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<RemoveGroupChatMember>
{
	public async Task<IResult> Handle(
		RemoveGroupChatMember request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, rawMemberId) = request;
		var memberId = new UserId(rawMemberId);

		if (memberId == currentUser.Id)
			return new ErrorResult("GroupChat.CannotRemoveSelf", "Use the leave endpoint to leave a group chat.");

		var members = await dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId
			            && (m.UserId == currentUser.Id || m.UserId == memberId)
			            && m.Chat!.Type == ChatType.Group)
			.Select(m => new { m.UserId, m.Role })
			.ToListAsync(cancellationToken);

		var currentMember = members.FirstOrDefault(m => m.UserId == currentUser.Id);
		var targetMember = members.FirstOrDefault(m => m.UserId == memberId);

		if (currentMember is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat does not exist.", 404);

		if (currentMember.Role == ChatParticipantRole.Member)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation.", 403);

		if (targetMember is null)
			return Results.NoContent();

		if (targetMember.Role != ChatParticipantRole.Member)
			return new ErrorResult("GroupChat.CannotRemoveAdmin", "Admins cannot be removed. Demote them first.", 403);

		await dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId
			            && m.UserId == memberId
			            && m.Role == ChatParticipantRole.Member)
			.ExecuteDeleteAsync(cancellationToken);

		return Results.NoContent();
	}
}