using Projects.Core.Entities;
using System.Text.Json.Serialization;

namespace Projects.Core.ReadModels;

internal class AssetReadModel
{
	public Guid Id { get; init; }
	public string Name { get; init; } = null!;
	public AssetType Type { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<AssetReadModel>? Children { get; init; } = new();
}