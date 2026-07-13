using Shop.Core.Entities.Catalog;

namespace Shop.Core.Entities.Funds;

public class Discount
{
	public Guid Id { get; init; }
	public int DiscountPercent { get; init; }
	public DateTimeOffset ValidFrom { get; init; }
	public DateTimeOffset ValidTo { get; init; }

	public ICollection<Game> Games { get; } = [];

	public bool IsActive(DateTimeOffset now) => now >= ValidFrom && now <= ValidTo;
}