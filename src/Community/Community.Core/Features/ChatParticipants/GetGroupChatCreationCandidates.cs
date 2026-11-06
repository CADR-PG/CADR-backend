using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Models.Chats;
using Community.Core.Models.Profiles;
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

internal record struct GetGroupChatCreationCandidates(
	CurrentUser CurrentUser,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 0,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetGroupChatCreationCandidatesEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetGroupChatCreationCandidates, GetGroupChatCreationCandidatesHandler>("group-chats/member-candidates")
		.Produces<OffsetPaginated<UserProfileSummaryReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetGroupChatCreationCandidatesHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetGroupChatCreationCandidates>
{
	public async Task<IResult> Handle(GetGroupChatCreationCandidates request, CancellationToken cancellationToken)
	{
		var (currentUser, searchPhrase, pageNumber, pageSize) = request;

		var baseDbQuery = dbContext.UserProfiles
			.AsNoTracking()
			.GetGroupChatMembersCandidates(dbContext, currentUser.Id, chatId: null);

		if (!string.IsNullOrWhiteSpace(searchPhrase))
		{
			baseDbQuery = baseDbQuery.Where(userProfile => EF.Functions.Like(userProfile.Name, $"%{searchPhrase}%"));
		}

		var finalDbQuery = baseDbQuery.Select(UserProfileSummaryReadModel.MappingExpression);

		var readModels = await OffsetPaginated.Create(finalDbQuery, pageNumber!.Value, pageSize!.Value);
		return Results.Ok(readModels);
	}
}