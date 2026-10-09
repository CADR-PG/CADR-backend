using Shared.ValueObjects;

namespace Shop.Core.Entities.Catalog;

public class GamePlays
{
	public Guid Id { get; set; }
	public Guid GameId { get; set; }
	public UserId UserId { get; set; }

	public DateTime FirstPlayedAt { get; set; }
	public DateTime LastPlayedAt { get; set; }


	public static GamePlays Create(Guid gameId, Guid userId, DateTime firstPlayedAt, DateTime lastPlayedAt)
	{
		var id = Guid.NewGuid();
		var play = new GamePlays()
		{
			Id = id,
			GameId = gameId,
			UserId = userId,
			FirstPlayedAt = firstPlayedAt,
			LastPlayedAt = lastPlayedAt
		};
		return play;
	}
}