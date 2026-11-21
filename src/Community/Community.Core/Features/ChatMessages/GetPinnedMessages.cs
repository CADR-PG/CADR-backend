using Community.Core.Database;
using Community.Core.Database.Extensions;
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

namespace Community.Core.Features.ChatMessages;

internal record struct GetPinnedMessages(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] ChatMessageId? BeforeMessageId,
	[FromQuery] ChatMessageId? AfterMessageId,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetPinnedMessagesEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetPinnedMessages, GetPinnedMessagesHandler>("chats/{chatId:long}/pinned-messages")
		.Produces<CursorPaginated<ChatMessagePinReadModel>>()
		.RequireAuthorization()
		.ProducesError(400, "`Messages.InvalidCursor`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`Chat.NotFound`");
}

internal sealed class GetPinnedMessagesHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetPinnedMessages>
{
	private const int DefaultPageSize = 50;
	private const int MaxPageSize = 100;

	public async Task<IResult> Handle(GetPinnedMessages request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, searchPhrase, beforeMessageId, afterMessageId, pageSizeParam) = request;
		var pageSize = Math.Clamp(pageSizeParam ?? DefaultPageSize, 1, MaxPageSize);

		if (beforeMessageId is not null && afterMessageId is not null)
			return new ErrorResult("Messages.InvalidCursor", "Use only one of: before, after.");

		var isActiveParticipant = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.AnyAsync(cancellationToken);

		if (!isActiveParticipant)
			return new ErrorResult("Chat.NotFound", "Chat not found.", 404);

		var cursorMessageId = afterMessageId ?? beforeMessageId;
		DateTimeOffset? cursorPinnedAt = null;

		if (cursorMessageId is { } cursorId)
		{
			cursorPinnedAt = await dbContext.ChatMessagePins
				.Where(pin => pin.ChatId == chatId && pin.MessageId == cursorId)
				.Select(pin => (DateTimeOffset?)pin.PinnedAt)
				.FirstOrDefaultAsync(cancellationToken);

			if (cursorPinnedAt is null)
				return new ErrorResult("Messages.InvalidCursor", "Cursor message is not pinned in this chat.");
		}

		var baseDbQuery = dbContext.ChatMessagePins
			.AsNoTracking()
			.Where(pin => pin.ChatId == chatId && pin.Message!.DeletedAt == null);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = LikePattern.Contains(searchPhrase);
			baseDbQuery = baseDbQuery.Where(pin => EF.Functions.ILike(pin.Message!.Content!, pattern, LikePattern.EscapeCharacter));
		}

		var isAscending = afterMessageId is not null;

		if (isAscending)
		{
			var (pinnedAt, messageId) = (cursorPinnedAt!.Value, cursorMessageId!.Value);
			baseDbQuery = baseDbQuery
				.Where(pin => pin.PinnedAt > pinnedAt || (pin.PinnedAt == pinnedAt && pin.MessageId > messageId))
				.OrderBy(pin => pin.PinnedAt)
				.ThenBy(pin => pin.MessageId);
		}
		else
		{
			if (cursorPinnedAt is { } pinnedAt && cursorMessageId is { } messageId)
				baseDbQuery = baseDbQuery.Where(pin => pin.PinnedAt < pinnedAt || (pin.PinnedAt == pinnedAt && pin.MessageId < messageId));

			baseDbQuery = baseDbQuery
				.OrderByDescending(pin => pin.PinnedAt)
				.ThenByDescending(pin => pin.MessageId);
		}

		var finalQuery = baseDbQuery.Select(ChatMessagePinReadModel.MappingExpression);

		var response = await CursorPaginated.Create(finalQuery, pageSize, reverse: isAscending, cancellationToken);
		return Results.Ok(response);
	}
}
