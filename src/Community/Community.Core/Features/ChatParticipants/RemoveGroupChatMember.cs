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
using Shared.ValueObjects;

namespace Community.Core.Features.ChatParticipants;

internal record struct RemoveGroupChatMember(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] UserId MemberId
) : IHttpRequest;

internal sealed class RemoveGroupChatMemberEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RemoveGroupChatMember, RemoveGroupChatMemberHandler>(
			"group-chats/{chatId:long}/members/{memberId:guid}")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.CannotRemoveSelf`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`, `GroupChat.CannotRemoveAdmin`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class RemoveGroupChatMemberHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<RemoveGroupChatMember>
{
	public async Task<IResult> Handle(
		RemoveGroupChatMember request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, memberId) = request;

		if (memberId == currentUser.Id)
			return new ErrorResult("GroupChat.CannotRemoveSelf", "Use the leave endpoint to leave a group chat.");

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		await dbContext.LockChatForUpdate(chatId, cancellationToken);

		var currentMemberRole = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(participant => participant.Chat!.Type == ChatType.Group)
			.Select(participant => (ChatParticipantRole?)participant.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (currentMemberRole is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat does not exist.", 404);

		if (currentMemberRole != ChatParticipantRole.Admin)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation.", 403);

		var targetMember = await dbContext.ChatParticipants
			.Active(chatId)
			.FirstOrDefaultAsync(participant => participant.UserId == memberId, cancellationToken);

		if (targetMember is null)
			return Results.NoContent();

		if (targetMember.Role != ChatParticipantRole.Member)
			return new ErrorResult("GroupChat.CannotRemoveAdmin", "Admins cannot be removed. Demote them to a member first.", 403);

		targetMember.Deactivate(timeProvider);
		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Results.NoContent();
	}
}
