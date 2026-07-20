using Shared.ValueObjects;
using Shop.Core.Entities.Catalog;
using Shop.Core.Entities.Shopping.Order;

namespace Shop.Core.Entities.Library;

public class LibraryEntry
{
	public Guid Id { get; init; }
	public UserId UserId { get; init; }
	public Guid GameId { get; init; }
	public Game Game { get; set; } = null!;
	public Guid OrderId { get; init; }
	public Order Order { get; set; } = null!;
	public DateTime AcquiredAt { get; init; }
}
