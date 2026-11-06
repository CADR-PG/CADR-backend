using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Models.Chats;
using Community.Core.Models.Profiles;
using Community.Core.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Extensions;
using Shared.Services;

namespace Community.Core.Features.ChatMessages;

internal record struct GetChatMessages(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] ChatMessageId? Around,
	[FromQuery] ChatMessageId? Before,
	[FromQuery] ChatMessageId? After,
	[FromQuery] int? Limit = 50
) : IHttpRequest;

internal sealed record GetChatMessagesResponse(
	IReadOnlyList<ChatMessageReadModel> Messages,
	IReadOnlyList<UserProfileSummaryReadModel> Senders
);

internal sealed class GetChatMessagesEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetChatMessages, GetChatMessagesHandler>("chats/{chatId}/messages")
		.Produces<GetChatMessagesResponse>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetChatMessagesHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetChatMessages>
{
	private const int DefaultLimit = 50;
	private const int MaxLimit = 100;

	public async Task<IResult> Handle(GetChatMessages request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, searchPhrase, around, before, after, limitParam) = request;
		var limit = Math.Clamp(limitParam ?? DefaultLimit, 1, MaxLimit);

		var cursorsCount = (around is not null ? 1 : 0)
			+ (before is not null ? 1 : 0)
			+ (after is not null ? 1 : 0);

		if (cursorsCount > 1)
			return new ErrorResult("Messages.InvalidCursor", "Use only one of: around, before, after.");

		var isActiveParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AnyAsync(cancellationToken);

		if (!isActiveParticipant)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		var baseDbQuery = dbContext.ChatMessages
			.AsNoTracking()
			.Where(m => m.ChatId == chatId);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = $"%{EscapeLike(searchPhrase.Trim())}%";
			baseDbQuery = baseDbQuery.Where(m => EF.Functions.ILike(m.Content!, pattern, "\\"));
		}

		List<ChatMessageReadModel> messages;

		if (around is { } aroundId)
		{
			var newerCount = limit / 2;
			var olderCount = limit - newerCount;

			var older = await baseDbQuery
				.Where(m => m.Id < aroundId)
				.OrderByDescending(m => m.Id)
				.Take(olderCount)
				.Select(ChatMessageReadModel.MappingExpression)
				.ToListAsync(cancellationToken);

			var newer = await baseDbQuery
				.Where(m => m.Id > aroundId)
				.OrderBy(m => m.Id)
				.Take(newerCount)
				.Select(ChatMessageReadModel.MappingExpression)
				.ToListAsync(cancellationToken);

			newer.Reverse();
			messages = [.. newer, .. older];
		}
		else if (after is { } afterId)
		{
			messages = await baseDbQuery
				.Where(m => m.Id > afterId)
				.OrderBy(m => m.Id)
				.Take(limit)
				.Select(ChatMessageReadModel.MappingExpression)
				.ToListAsync(cancellationToken);

			messages.Reverse();
		}
		else
		{
			if (before is { } beforeId)
				baseDbQuery = baseDbQuery.Where(m => m.Id < beforeId);

			messages = await baseDbQuery
				.OrderByDescending(m => m.Id)
				.Take(limit)
				.Select(ChatMessageReadModel.MappingExpression)
				.ToListAsync(cancellationToken);
		}

		var senderIds = messages
			.Select(m => m.SenderId)
			.Distinct()
			.ToList();

		var senders = senderIds.Count > 0
			? await dbContext.UserProfiles
				.Where(u => senderIds.Contains(u.UserId))
				.Select(UserProfileSummaryReadModel.MappingExpression)
				.ToListAsync(cancellationToken)
			: [];

		return Results.Ok(new GetChatMessagesResponse(messages, senders));
	}

	private static string EscapeLike(string value) => value
		.Replace("\\", "\\\\")
		.Replace("%", "\\%")
		.Replace("_", "\\_");
}