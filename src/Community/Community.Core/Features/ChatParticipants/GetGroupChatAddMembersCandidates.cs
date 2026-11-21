using Community.Core.Database;
using Community.Core.Services;
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
	[FromQuery] int? PageNumber = 1,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetGroupChatAddMembersCandidatesEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetGroupChatAddMembersCandidates, GetGroupChatAddMembersCandidatesHandler>("group-chats/{chatId:long}/member-candidates")
		.Produces<OffsetPaginated<UserProfileSummaryReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(403, "`GroupChat.Forbidden`")
		.ProducesError(404, "`GroupChat.NotFound`");
}

internal sealed class GetGroupChatAddMembersCandidatesHandler(
	CommunityDbContext dbContext,
	UserProfilesSynchronizer userProfilesSynchronizer
) : IHttpRequestHandler<GetGroupChatAddMembersCandidates>
{
	public async Task<IResult> Handle(GetGroupChatAddMembersCandidates request, CancellationToken cancellationToken)
	{
		var (currentUser, chatId, searchPhrase, pageNumber, pageSize) = request;

		var currentMemberRole = await dbContext.ChatParticipants
			.Where(ChatParticipantsQueries.IsActive(chatId, currentUser.Id))
			.Where(chatParticipant => chatParticipant.Chat!.Type == ChatType.Group)
			.Select(chatParticipant => (ChatParticipantRole?)chatParticipant.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (currentMemberRole is null)
			return new ErrorResult("GroupChat.NotFound", "Group chat with given ID does not exist.", 404);

		if (currentMemberRole != ChatParticipantRole.Admin)
			return new ErrorResult("GroupChat.Forbidden", "Only group admins can perform this operation.", 403);

		await userProfilesSynchronizer.EnsureAllSynchronized(cancellationToken);

		var baseDbQuery = dbContext.UserProfiles
			.AsNoTracking()
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			var pattern = LikePattern.Contains(searchPhrase);
			baseDbQuery = baseDbQuery.Where(userProfile => EF.Functions.ILike(userProfile.Name, pattern, LikePattern.EscapeCharacter));
		}

		var finalDbQuery = baseDbQuery
			.OrderBy(userProfile => userProfile.Name)
			.ThenBy(userProfile => userProfile.UserId)
			.Select(UserProfileSummaryReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber, pageSize, cancellationToken);
		return Results.Ok(readModels);
	}
}