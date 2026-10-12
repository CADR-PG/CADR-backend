using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Services;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;
using Shop.Core.ReadModels;

namespace Shop.Core.Features;

internal record struct GetMostPopularGames() : IHttpRequest;

internal sealed class GetMostPopularGamesEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapGet<GetMostPopularGames, GetMostPopularGamesHandler>("get-most-popular-games")
			.RequireAuthorization()
			.Produces<Paginated<GameShopCardReadModel>>(200)
			.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetMostPopularGamesHandler(ShopDbContext dbContext) : IHttpRequestHandler<GetMostPopularGames>
{
	public async Task<IResult> Handle(GetMostPopularGames request, CancellationToken cancellationToken)
	{
		var since = DateTime.UtcNow.AddDays(-7);

		var topPlaysQuery = dbContext.GamePlays
			.Where(p => p.LastPlayedAt >= since)
			.GroupBy(p => p.GameId)
			.Select(g => new { GameId = g.Key, PlaysCount = g.Count() })
			.OrderByDescending(x => x.PlaysCount)
			.ThenBy(x => x.GameId);

		var topPlaysPage = await Paginated.Create(topPlaysQuery, pageSize: 10);

		var gameIds = topPlaysPage.Items.Select(x => x.GameId).ToList();
		if (gameIds.Count == 0)
		{
			var randomGames = await dbContext.Games
				.Include(g => g.Price)
				.Where(g => g.State == GameStates.Published)
				.OrderBy(_ => EF.Functions.Random())
				.Take(10)
				.ToListAsync(cancellationToken);

			var randomCards = randomGames
				.Select(g => GameShopCardReadModel.From(g, 0))
				.ToList();

			return Results.Ok(new Paginated<GameShopCardReadModel>(randomCards, randomCards.Count, 1, 10));
		}
		var games = await dbContext.Games
			.Include(g => g.Price)
			.Where(g => gameIds.Contains(g.Id))
			.ToDictionaryAsync(g => g.Id, cancellationToken);

		var mostPopularGames = topPlaysPage.Items
			.Where(x => games.ContainsKey(x.GameId))
			.Select(x => GameShopCardReadModel.From(games[x.GameId], x.PlaysCount))
			.ToList();

		var paginatedPopularGames = new Paginated<GameShopCardReadModel>(
			mostPopularGames,
			topPlaysPage.TotalCount,
			topPlaysPage.PageNumber,
			topPlaysPage.PageSize);

		return Results.Ok(paginatedPopularGames);
	}
}