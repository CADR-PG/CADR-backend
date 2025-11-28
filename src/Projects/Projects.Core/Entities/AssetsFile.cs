using Microsoft.CodeAnalysis.Operations;

namespace Projects.Core.Entities;

public class AssetsFile
{
	public required Guid Id { get; init; }
	public required string Name { get; set; }
	public required uint SizeInBytes { get; set; }

	public required DateTime CreatedAt { get; init; }
	public DateTime? LastModifiedAt { get; set; }

	public required Guid ProjectId { get; init; }
	public required Guid? DirectoryId { get; set; }

	public string BlobResourceName => $"{ProjectId}/{DirectoryId}";

	public const string BlobContainerName = "project-assets-files";

	public static AssetsFile Create(Guid projectId, Guid directoryId, string name, uint sizeInBytes) => new()
	{
		Id = Guid.NewGuid(),
		Name = name,
		SizeInBytes = sizeInBytes,
		CreatedAt = DateTime.UtcNow,
		ProjectId = projectId,
		DirectoryId = directoryId,
	};
}