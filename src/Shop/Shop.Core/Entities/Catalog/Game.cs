using Microsoft.AspNetCore.Http.HttpResults;
using Shared.ValueObjects;
using Shop.Core.Entities.Funds;

namespace Shop.Core.Entities.Catalog;

public class Game
{
	public Guid Id { get; init; }
	required public string Title { get; init; }
	required public string Description { get; init; }
	required public Price Price { get; init; }
	public UserId AuthorId { get; init; }
	public DateTime CreatedAt { get; init; }
	public DateTime? UpdatedAt { get; init; }
	public int AgeRestriction { get; init; }
	public GameStates State { get; init; }

	public ICollection<Discount> Discounts { get; } = [];
	public ICollection<Genre> Genres { get; } = [];
	public ICollection<Review> Reviews { get; } = [];
	public ICollection<GameVersion> Versions { get; } = []; // opisy tego co sie zmienilo i odniesienia do poszczegolnych wersji gry

	public static Game Create(string title, string description, Price price, int ageRestriction, GameStates state, UserId authorId) => new()
	{
		Id = Guid.NewGuid(),
		Title = title,
		Description = description,
		Price = price,
		CreatedAt = DateTime.UtcNow,
		UpdatedAt = DateTime.UtcNow,
		AgeRestriction = ageRestriction,
		State = state,
		AuthorId = authorId
	};
}