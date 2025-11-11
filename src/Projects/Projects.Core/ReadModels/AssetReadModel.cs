using Projects.Core.Entities;

namespace Projects.Core.ReadModels;

internal record AssetReadModel(Guid Id, string? Name)
{
	public static AssetReadModel From(Asset asset) =>
		new(asset.Id, asset.Name);
}