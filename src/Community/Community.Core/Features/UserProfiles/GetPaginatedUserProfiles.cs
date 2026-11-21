using Community.Core.Database;
using Community.Core.Services;
using Community.Core.Database.Extensions;
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

namespace Community.Core.Features.UserProfiles;

internal record struct GetPaginatedUserProfiles(
	CurrentUser CurrentUser,
	[FromQuery] string? SearchPhrase,
	[FromQuery] int? PageNumber = 1,
	[FromQuery] int? PageSize = 50
) : IHttpRequest;

internal sealed class GetPaginatedUserProfilesEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetPaginatedUserProfiles, GetPaginatedUserProfilesHandler>("user-profiles")
		.Produces<OffsetPaginated<UserProfileSummaryReadModel>>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetPaginatedUserProfilesHandler(
	CommunityDbContext dbContext,
	UserProfilesSynchronizer userProfilesSynchronizer
) : IHttpRequestHandler<GetPaginatedUserProfiles>
{
	public async Task<IResult> Handle(GetPaginatedUserProfiles request, CancellationToken cancellationToken)
	{
		var (currentUser, searchPhrase, pageNumber, pageSize) = request;

		await userProfilesSynchronizer.EnsureAllSynchronized(cancellationToken);

		var baseDbQuery = dbContext.UserProfiles
			.AsNoTracking()
			.Where(userProfile => userProfile.UserId != currentUser.Id)
			.Where(userProfile => !dbContext.UserBlocks.Any(userBlock =>
				(userBlock.BlockerId == currentUser.Id && userBlock.BlockedId == userProfile.UserId) ||
				(userBlock.BlockerId == userProfile.UserId && userBlock.BlockedId == currentUser.Id)));

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
