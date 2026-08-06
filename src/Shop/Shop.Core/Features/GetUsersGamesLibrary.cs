using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Services;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Features;

internal record struct GetUsersGamesLibrary(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetUsersGamesLibraryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapGet<GetUsersGamesLibrary, GetUsersGamesLibraryHandler>("my-library").RequireAuthorization();
}

internal sealed class GetUsersGamesLibraryHandler(
	ShopDbContext dbContext
) : IHttpRequestHandler<GetUsersGamesLibrary>
{
	public async Task<IResult> Handle(GetUsersGamesLibrary request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;
		var usersLibraryQuery = dbContext.LibraryEntries
			.Where(entry => entry.UserId == userId)
			.Select(entry => entry.Game);

		var paginatedListOfGames = await Paginated.Create(usersLibraryQuery);
		return Results.Ok(paginatedListOfGames);
	}
}