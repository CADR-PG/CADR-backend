using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
using Community.Core.Models.Chats;
using Community.Core.Services;
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
using Shared.Services;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Community.Core.Features.ChatParticipants;

internal record struct AddGroupChatMembers(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	AddGroupChatMembers.Body Payload
) : IHttpRequest
{
	internal sealed record Body(
		[property: Required][property: MinLength(1)][property: MaxLength(AddGroupChatMembersHandler.MaxMembers)] List<UserId> MemberIds
	);
}

internal sealed class AddGroupChatMembersEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddGroupChatMembers, AddGroupChatMembersHandler>("group-chats/{chatId:long}/members")
		.AddDataAnnotationsValidation<AddGroupChatMembers.Body>()
		.Produces<Listed<ChatParticipantReadModel>>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `GroupChat.TooManyMembers`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class AddGroupChatMembersHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider,
	UserProfilesSynchronizer userProfilesSynchronizer
) : IHttpRequestHandler<AddGroupChatMembers>
{
	public const int MaxMembers = 10;

	public async Task<IResult> Handle(
		AddGroupChatMembers request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId, payload) = request;

		var requestedMemberIds = payload.MemberIds
			.Where(memberId => memberId != currentUser.Id)
			.Distinct()
			.ToList();

		await userProfilesSynchronizer.EnsureSynchronized(requestedMemberIds, cancellationToken);

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		await dbContext.LockChatForUpdate(chatId, cancellationToken);

		var currentMemberRole = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(participant => participant.Chat!.Type == ChatType.Group)
			.Select(participant => (ChatParticipantRole?)participant.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (currentMemberRole is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat not found.", 404);

		if (currentMemberRole != ChatParticipantRole.Admin)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation.", 403);

		var finalMemberIds = await dbContext.UserProfiles
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId: chatId)
			.Where(userProfile => requestedMemberIds.Contains(userProfile.UserId))
			.Select(userProfile => userProfile.UserId)
			.ToListAsync(cancellationToken);

		var activeMembersCount = await dbContext.ChatParticipants
			.Active(chatId)
			.CountAsync(cancellationToken);

		if (activeMembersCount + finalMemberIds.Count > MaxMembers)
			return new ErrorResult("GroupChat.TooManyMembers", $"A group can have at most {MaxMembers} members.");

		var joinedAt = timeProvider.GetUtcNow();

		var deactivatedMembers = await dbContext.ChatParticipants
			.Where(participant => participant.ChatId == chatId
								  && participant.DeactivatedAt != null
								  && finalMemberIds.Contains(participant.UserId))
			.ToListAsync(cancellationToken);

		foreach (var deactivatedMember in deactivatedMembers)
			deactivatedMember.Reactivate(joinedAt);

		var newMembers = finalMemberIds
			.Except(deactivatedMembers.Select(member => member.UserId))
			.Select(memberId => ChatParticipant.CreateParticipant(chatId, memberId, joinedAt));

		await dbContext.ChatParticipants.AddRangeAsync(newMembers, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		var allChatMembersQuery = dbContext.ChatParticipants
			.AsNoTracking()
			.Active(chatId)
			.OrderBy(participant => participant.JoinedAt)
			.Select(ChatParticipantReadModel.MappingExpression);

		var response = await Listed.Create(allChatMembersQuery, cancellationToken);
		return Results.Ok(response);
	}
}