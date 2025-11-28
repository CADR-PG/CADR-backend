namespace Projects.Core.Entities;

public sealed class AssetsDirectory
{
	private const string RootDirectoryName = "/root";

	private AssetsDirectory() { }

	public required Guid Id { get; init; }
	public required string Name { get; set; }

	public required DateTime CreatedAt { get; init; }

	public DateTime? LastModifiedAt { get; set; }

	public required Guid ProjectId { get; init; }
	public required Guid? DirectoryId { get; set; }

	public bool IsRoot => DirectoryId.HasValue;

	public ICollection<AssetsFile> Files { get; init; } = [];

	public static AssetsDirectory Create(Guid projectId, string name, Guid directoryId) => new()
	{
		Id = Guid.NewGuid(),
		Name = name,
		CreatedAt = DateTime.UtcNow,
		ProjectId = projectId,
		DirectoryId = directoryId
	};

	public static AssetsDirectory CreateRoot(Guid projectId)=> new()
	{
		Id = Guid.NewGuid(),
		Name = RootDirectoryName,
		CreatedAt = DateTime.UtcNow,
		ProjectId = projectId,
		DirectoryId = null
	};

}