namespace Shop.Core.ReadModels;

public class GameAssetsReadModel
{
	public required string Path { get; init; }
	public required Uri Url { get; init; }
}