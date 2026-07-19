using Shared.ValueObjects;
using Shop.Core.Entities.Funds;

namespace Shop.Core.Entities.Catalog;

public class Game
{
	public Guid Id { get; init; }
	required public String Title { get; init; }
	required public String Description { get; init; }
	required public Price Price { get; init; }
	public UserId AuthorId { get; init; }
	public Guid GameId { get; init; }
	public DateTime CreatedAt { get; init; }
	public DateTime? UpdatedAt { get; init; }
	public bool IsPublished { get; init; }

	public ICollection<Discount> Discounts { get; } = [];
	public ICollection<Genre> Genres { get; } = [];
	public ICollection<Review> Reviews { get; } = [];
}