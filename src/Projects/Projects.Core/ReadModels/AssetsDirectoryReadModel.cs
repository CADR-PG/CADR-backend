using Projects.Core.Entities;

namespace Projects.Core.ReadModels;

public sealed class AssetsDirectoryReadModel
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime? LastModifiedAt { get; init; }

	public static AssetsDirectoryReadModel From(AssetsDirectory directory) => new()
	{
		Id = directory.Id,
		Name = directory.Name,
		CreatedAt = directory.CreatedAt,
		LastModifiedAt = directory.LastModifiedAt
	};
}