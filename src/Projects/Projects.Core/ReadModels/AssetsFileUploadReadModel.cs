using Projects.Core.Entities;

namespace Projects.Core.ReadModels;

public sealed class AssetsFileUploadReadModel
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public required uint SizeInBytes { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime? LastModifiedAt { get; init; }
	public required Uri UploadUrl { get; init; }

	public static AssetsFileUploadReadModel From(AssetsFile file, Uri uploadUrl) => new()
	{
		Id = file.Id,
		Name = file.Name,
		SizeInBytes = file.SizeInBytes,
		CreatedAt = file.CreatedAt,
		LastModifiedAt = file.LastModifiedAt,
		UploadUrl = uploadUrl
	};
}