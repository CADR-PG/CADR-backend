using Shop.Core.Entities.Catalog;

namespace Shop.Core.Entities.Funds;

public class Discount
{
	public Guid Id { get; init; }
	public int DiscountPercent { get; init; }
	public DateTime ValidFrom { get; init; }
	public DateTime ValidTo { get; init; }

	public ICollection<Game> Games { get; } = [];

	public bool IsActive(DateTime now) => now >= ValidFrom && now <= ValidTo;
}