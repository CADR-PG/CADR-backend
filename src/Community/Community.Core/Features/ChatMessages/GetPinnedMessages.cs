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
using Shared.Extensions;
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
		.MapGet<GetPinnedMessages, GetPinnedMessagesHandler>("chats/{chatId}/pinned-messages")
		.Produces<Listed<ChatReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
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

		var baseDbQuery = dbContext.ChatMessages
			.AsNoTracking()
			.Where(m => m.ChatId == chatId);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(m => EF.Functions.ILike(m.Content!, searchPhrase, "\\"));
		}

		var isAscending = afterMessageId is not null;

		if (afterMessageId is { } afterId)
			baseDbQuery = baseDbQuery.Where(chatMessage => chatMessage.Id > afterId).OrderBy(m => m.Id);
		else
		{
			if (beforeMessageId is { } beforeId)
				baseDbQuery = baseDbQuery.Where(chatMessage => chatMessage.Id < beforeId);

			baseDbQuery = baseDbQuery.OrderByDescending(m => m.Id);
		}

		var finalQuery = baseDbQuery
			.Select(ChatMessageReadModel.MappingExpression);

		var response = CursorPaginated.Create(finalQuery, pageSize, reverse: isAscending);

		return Results.Ok(response);
	}
}