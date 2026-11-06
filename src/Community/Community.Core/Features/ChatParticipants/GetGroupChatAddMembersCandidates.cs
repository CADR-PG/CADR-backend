using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.Chats;
using Community.Core.Entities.UserProfiles;
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
using Shared.Services;

namespace Community.Core.Features.ChatParticipants;

internal record struct GetGroupChatAddMembersCandidates(
	CurrentUser CurrentUser,
	[FromRoute] ChatId ChatId,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetGroupChatAddMembersCandidatesEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetGroupChatAddMembersCandidates, GetGroupChatAddMembersCandidatesHandler>("group-chats/{chatId}/member-candidates")
		.Produces<OffsetPaginated<UserProfileSummaryReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class GetGroupChatAddMembersCandidatesHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetGroupChatAddMembersCandidates>
{
	public async Task<IResult> Handle(GetGroupChatAddMembersCandidates request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, searchPhrase, pageNumber, pageSize) = request;

		var isGroupChatMember = await dbContext.ChatParticipants.AnyAsync(chatParticipant => chatParticipant.UserId == currentUser.Id && chatParticipant.ChatId == chatId && chatParticipant.Chat!.Type == ChatType.Group, cancellationToken);
		if (!isGroupChatMember)
			return new ErrorResult("GroupChat.NotFound", "Group chat with given ID does not exists!", 404);

		var baseDbQuery = dbContext.UserProfiles
			.AsNoTracking()
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(userProfile => EF.Functions.Like(userProfile.Name, $"%{searchPhrase}%"));
		}

		var finalDbQuery = baseDbQuery.Select(UserProfileSummaryReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}