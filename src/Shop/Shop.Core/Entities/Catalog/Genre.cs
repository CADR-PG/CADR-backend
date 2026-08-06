namespace Shop.Core.Entities.Catalog;

public class Genre
{
	public Guid Id { get; init; }
	public required string Name { get; init; }

	public ICollection<Game> Games { get; } = [];
}