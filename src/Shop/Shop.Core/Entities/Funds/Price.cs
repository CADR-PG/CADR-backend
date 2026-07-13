

using Shop.Core.Entities.Catalog;

namespace Shop.Core.Entities.Funds;

public class Price
{
	public Guid GameId { get; init; }
	public Game Game { get; set; } = null!;
	public decimal Amount { get; init; }
	public required Currency Currency { get; init; }
	public DateTimeOffset ValidFrom { get; init; }
}