using Shop.Core.Entities.Catalog;

namespace Shop.Core.ReadModels;

internal sealed class GameReadModel
{
	public required Guid Id { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required decimal Amount { get; init; }
	public required string Currency { get; init; }
	public required int AgeRestriction { get; init; }
	public required GameStates State { get; init; }
	public required Guid? ActiveVersionId { get; init; }

	public static GameReadModel From(Game game) => new()
	{
		Id = game.Id,
		Title = game.Title,
		Description = game.Description,
		Amount = game.Price.Amount,
		Currency = game.Price.Currency,
		AgeRestriction = game.AgeRestriction,
		State = game.State,
		ActiveVersionId = game.ActiveVersionId
	};
}