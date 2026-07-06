using Shared.ValueObjects;

namespace Shop.Core.Entities;

public class Game
{
	public Guid Id { get; init; }
	required public String Title { get; init; }
	required public String Description { get; init; }
	required public decimal Price { get; init; }
	public UserId AuthorId { get; init; }
	public Guid GameId { get; init; }
	public DateTime CreatedAt { get; init; }
	public DateTime UpdatedAt { get; init; }
}