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
	public GameStates State { get; set; }
	public Guid? ActiveVersionId { get; set; }

	public ICollection<Discount> Discounts { get; } = [];
	public ICollection<Genre> Genres { get; } = [];
	public ICollection<Review> Reviews { get; } = [];
	public ICollection<GameVersion> Versions { get; } = []; // opisy tego co sie zmienilo i odniesienia do poszczegolnych wersji gry

	public static Game Create(string title, string description, decimal amount, string currnecy, int ageRestriction, GameStates state, UserId authorId)
	{
		var id = Guid.NewGuid();
		var game = new Game
		{
			Id = id,
			Title = title,
			Description = description,
			Price = new Price()
			{
				Amount = amount,
				Currency = currnecy,
				GameId = id,
				ValidFrom = DateTime.UtcNow,
			},
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			AgeRestriction = ageRestriction,
			State = state,
			AuthorId = authorId
		};
		return game;
	}

}