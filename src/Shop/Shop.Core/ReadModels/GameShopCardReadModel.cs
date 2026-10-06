using Shop.Core.Entities.Catalog;

namespace Shop.Core.ReadModels;

internal sealed class GameShopCardReadModel
{
	public required Guid GameId { get; init; }
	public required string Title { get; init; }
	public required decimal Amount { get; init; }
	public required string Currency { get; init; }
	public required int PlaysCount { get; init; }

	public static GameShopCardReadModel From(Game game, int playsCount) => new()
	{
		GameId = game.Id,
		Title = game.Title,
		Amount = game.Price.Amount,
		Currency = game.Price.Currency,
		PlaysCount = playsCount
	};
}