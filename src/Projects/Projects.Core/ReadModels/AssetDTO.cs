using Projects.Core.Entities;
using System.Text.Json.Serialization;

namespace Projects.Core.ReadModels;

internal class AssetDTO
{
	public Guid  Id { get; init; }
	public string Name { get; init; } = null!;
	public AssetType Type { get; init; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<AssetDTO>? Children { get; init; } = new();
}