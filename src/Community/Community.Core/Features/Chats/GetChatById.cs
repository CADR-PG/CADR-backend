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

namespace Community.Core.Features.Chats;

internal record struct GetChatById(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId
) : IHttpRequest;

internal sealed class GetChatByIdEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetChatById, GetChatByIdHandler>("chats/{chatId:long}")
		.Produces<ChatReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`Chat.NotFound`");
}

internal sealed class GetChatByIdHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetChatById>
{
	public async Task<IResult> Handle(GetChatById request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId) = request;

		var dbQuery = dbContext.Chats
			.AsNoTracking()
			.Where(chat => chat.Id == chatId)
			.AccessibleBy(currentUser.Id)
			.Select(ChatReadModel.GetMappingExpression(currentUser.Id, dbContext));

		var readModel = await dbQuery.FirstOrDefaultAsync(cancellationToken);
		return readModel is null
			? new ErrorResult("Chat.NotFound", "Chat with given ID does not exist.", 404)
			: Results.Ok(readModel);
	}
}