namespace Shop.Core.ReadModels;

public class PlayGameReadModel
{
	public required Guid GameId { get; init; }
	public required Guid VersionId { get; init; }
	public required DateTimeOffset ExpiresOn { get; init; }
	public required IReadOnlyList<GameAssetsReadModel> Assets { get; init; }
}