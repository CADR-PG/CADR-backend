using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shop.Core.Database;
using Shop.Core.Entities.Shopping.Order;
using Shop.Core.Services;

namespace Shop.Core.Features;

internal sealed record BuyGame([FromBody] BuyGame.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	internal record Data(Guid GameId);
}

internal sealed class BuyGameEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<BuyGame, BuyGameHandler>("buy-game")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError")
		.ProducesError(404, "`NotFoundError`");
}

internal sealed class BuyGameHandler(
	ShopDbContext dbContext,
	LibraryService libraryService
	) : IHttpRequestHandler<BuyGame>
{
	public async Task<IResult> Handle(BuyGame request, CancellationToken cancellationToken)
	{
		var gameId = request.Body.GameId;
		var userId = request.CurrentUser.Id;

		var game = await dbContext.Games.Include(g => g.Price).FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
		var isGameInLibrary = await dbContext.LibraryEntries.AnyAsync(l => l.GameId == gameId && l.UserId == userId, cancellationToken);
		var order = new Order { Id = Guid.NewGuid() };
		if (isGameInLibrary)
			return Results.Problem("Game already in library");
		if (game is null)
			return Results.Problem("Game not found");
		if (game.Price.IsFree)
		{
			await dbContext.Orders.AddAsync(order, cancellationToken);
			await libraryService.GrantAccessToGame(userId, gameId, order.Id, cancellationToken);
			return Results.Ok();
		}

		var wallet = await dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.CurrentUser.Id, cancellationToken);
		if (wallet is null)
			return Results.Problem("Wallet is not initialized");

		var price = game.Price.ToStripeUnitAmount();

		if (wallet.Ballance < price)
			return Results.Problem("You don't have enough funds to buy this game");

		wallet.Ballance -= game.Price.ToStripeUnitAmount();

		await dbContext.Orders.AddAsync(order, cancellationToken);
		await libraryService.GrantAccessToGame(userId, gameId, order.Id, cancellationToken);

		return Results.Ok();
	}
}