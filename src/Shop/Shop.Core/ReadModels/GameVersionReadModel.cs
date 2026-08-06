using Shop.Core.Entities.Catalog;

namespace Shop.Core.ReadModels;

internal sealed class GameVersionReadModel
{
	public required Guid Id { get; init; }
	public required Guid GameId { get; init; }
	public required string Version { get; init; }
	public required string? Description { get; init; }
	public required DateTime CreatedAt { get; init; }

	public static GameVersionReadModel From(GameVersion gameVersion) => new()
	{
		Id = gameVersion.Id,
		GameId = gameVersion.GameId,
		Version = gameVersion.Version,
		Description = gameVersion.Description,
		CreatedAt = gameVersion.CreatedAt
	};
}