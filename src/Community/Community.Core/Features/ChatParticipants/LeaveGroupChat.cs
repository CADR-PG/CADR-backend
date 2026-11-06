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

internal record struct LeaveGroupChat(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class LeaveGroupChatEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<LeaveGroupChat, LeaveGroupChatHandler>("group-chats/{chatId}/leave")
		.RequireAuthorization()
		.ProducesError(400, "`GroupChat.NotFound`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class LeaveGroupChatHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<LeaveGroupChat>
{
	public async Task<IResult> Handle(
		LeaveGroupChat request,
		CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var role = await dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId
			            && m.UserId == currentUser.Id
			            && m.Chat!.Type == ChatType.Group)
			.Select(m => (ChatParticipantRole?)m.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (role is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat does not exist.", 404);

		if (role == ChatParticipantRole.Member)
		{
			await dbContext.ChatParticipants
				.Where(m => m.ChatId == chatId && m.UserId == currentUser.Id)
				.ExecuteDeleteAsync(cancellationToken);

			return Results.NoContent();
		}

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		var otherMembers = dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId && m.UserId != currentUser.Id);

		var hasOtherAdmin = await otherMembers
			.AnyAsync(m => m.Role == ChatParticipantRole.Admin, cancellationToken);

		if (!hasOtherAdmin)
		{
			var successorId = await otherMembers
				.OrderBy(m => m.JoinedAt)
				.ThenBy(m => m.UserId)
				.Select(m => (UserId?)m.UserId)
				.FirstOrDefaultAsync(cancellationToken);

			if (successorId is null)
			{
				await dbContext.Chats
					.Where(c => c.Id == chatId)
					.ExecuteDeleteAsync(cancellationToken);

				await transaction.CommitAsync(cancellationToken);
				return Results.NoContent();
			}

			await dbContext.ChatParticipants
				.Where(m => m.ChatId == chatId && m.UserId == successorId)
				.ExecuteUpdateAsync(
					setters => setters.SetProperty(m => m.Role, ChatParticipantRole.Admin),
					cancellationToken);
		}

		await dbContext.ChatParticipants
			.Where(m => m.ChatId == chatId && m.UserId == currentUser.Id)
			.ExecuteDeleteAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);
		return Results.NoContent();
	}
}