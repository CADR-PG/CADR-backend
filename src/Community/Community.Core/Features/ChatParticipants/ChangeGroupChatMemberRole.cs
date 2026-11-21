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

internal record struct PromoteGroupChatMember(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] UserId MemberId
) : IHttpRequest;

internal record struct DemoteGroupChatMember(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromRoute] UserId MemberId
) : IHttpRequest;

internal sealed class PromoteGroupChatMemberEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<PromoteGroupChatMember, PromoteGroupChatMemberHandler>("group-chats/{chatId:long}/members/{memberId:guid}/promote")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`, `GroupChat.MemberNotFound`");
}

internal sealed class DemoteGroupChatMemberEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<DemoteGroupChatMember, DemoteGroupChatMemberHandler>("group-chats/{chatId:long}/members/{memberId:guid}/demote")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.LastAdmin`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`, `GroupChat.MemberNotFound`");
}

internal sealed class PromoteGroupChatMemberHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<PromoteGroupChatMember>
{
	public Task<IResult> Handle(PromoteGroupChatMember request, CancellationToken cancellationToken)
		=> GroupChatMemberRoleChanger.Change(dbContext, request.CurrentUser, request.ChatId, request.MemberId, ChatParticipantRole.Admin, cancellationToken);
}

internal sealed class DemoteGroupChatMemberHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<DemoteGroupChatMember>
{
	public Task<IResult> Handle(DemoteGroupChatMember request, CancellationToken cancellationToken)
		=> GroupChatMemberRoleChanger.Change(dbContext, request.CurrentUser, request.ChatId, request.MemberId, ChatParticipantRole.Member, cancellationToken);
}

internal static class GroupChatMemberRoleChanger
{
	public static async Task<IResult> Change(
		CommunityDbContext dbContext,
		CurrentUser currentUser,
		ChatId chatId,
		UserId memberId,
		ChatParticipantRole newRole,
		CancellationToken cancellationToken)
	{
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
			return new ErrorResult("GroupChat.MemberNotFound", "User is not a member of this group chat.", 404);

		if (targetMember.Role == newRole)
			return Results.NoContent();

		if (newRole == ChatParticipantRole.Member)
		{
			var otherAdminExists = await dbContext.ChatParticipants
				.Active(chatId)
				.AnyAsync(participant => participant.UserId != memberId && participant.Role == ChatParticipantRole.Admin, cancellationToken);

			if (!otherAdminExists)
				return new ErrorResult("GroupChat.LastAdmin", "Group chat must have at least one admin.");

			targetMember.DemoteToMember();
		}
		else
		{
			targetMember.PromoteToAdmin();
		}

		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Results.NoContent();
	}
}