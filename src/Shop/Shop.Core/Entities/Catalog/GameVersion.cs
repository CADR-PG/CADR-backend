namespace Shop.Core.Entities.Catalog;

public class GameVersion
{
	public required Guid Id { get; init; }
	public required Guid GameId { get; init; }
	public Game Game { get; init; } = null!;

	public required Guid ProjectId { get; init; }
	public required string Version { get; init; } = null!;
	public string? Description { get; init; }
	public required DateTime CreatedAt { get; init; }

	public string BlobResourceName => $"{GameId}/{Id}";
	public const string BlobContainerName = "games-snapshots";
}