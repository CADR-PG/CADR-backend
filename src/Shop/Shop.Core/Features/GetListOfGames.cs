using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Services;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Features;

internal record struct GetListOfGames() : IHttpRequest;

internal sealed class GetListOfGamesEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapGet<GetListOfGames, GetListOfGamesHandler>("").RequireAuthorization();
}

internal sealed class GetListOfGamesHandler(
	ShopDbContext dbContext
) : IHttpRequestHandler<GetListOfGames>
{
	public async Task<IResult> Handle(GetListOfGames request, CancellationToken cancellationToken)
	{
		var gamesList = dbContext.Games.Where(x => x.State == GameStates.Published);
		var paginatedListOfGames = await Paginated.Create(gamesList);
		return Results.Ok(paginatedListOfGames);
	}
}