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

namespace Community.Core.Features.ChatParticipants;

internal record struct LeaveGroupChat(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class LeaveGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<LeaveGroupChat, LeaveGroupChatHandler>("group-chats/{chatId:long}/leave")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class LeaveGroupChatHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<LeaveGroupChat>
{
	public async Task<IResult> Handle(
		LeaveGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		await dbContext.LockChatForUpdate(chatId, cancellationToken);

		var currentMember = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(participant => participant.Chat!.Type == ChatType.Group)
			.Include(participant => participant.Chat)
			.FirstOrDefaultAsync(cancellationToken);

		if (currentMember is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat does not exist.", 404);

		if (currentMember.Role == ChatParticipantRole.Admin)
		{
			var otherActiveMembers = dbContext.ChatParticipants
				.Active(chatId)
				.Where(participant => participant.UserId != currentUser.Id);

			var hasOtherAdmin = await otherActiveMembers
				.AnyAsync(participant => participant.Role == ChatParticipantRole.Admin, cancellationToken);

			if (!hasOtherAdmin)
			{
				var successor = await otherActiveMembers
					.OrderBy(participant => participant.JoinedAt)
					.ThenBy(participant => participant.UserId)
					.FirstOrDefaultAsync(cancellationToken);

				if (successor is null)
					currentMember.Chat!.MarkAsDeleted(currentUser.Id, timeProvider);
				else
					successor.PromoteToAdmin();
			}
		}

		currentMember.Deactivate(timeProvider);

		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return Results.NoContent();
	}
}
