using Projects.Core.Entities;

namespace Projects.Core.ReadModels;

public class AssetsFileDownloadReadModel
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public required uint SizeInBytes { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime? LastModifiedAt { get; init; }
	public required Uri DownloadUrl { get; init; }

	public static AssetsFileDownloadReadModel From(AssetsFile file, Uri downloadUrl) => new()
	{
		Id = file.Id,
		Name = file.Name,
		SizeInBytes = file.SizeInBytes,
		CreatedAt = file.CreatedAt,
		LastModifiedAt = file.LastModifiedAt,
		DownloadUrl = downloadUrl
	};
}