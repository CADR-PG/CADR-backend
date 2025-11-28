namespace Projects.Core.ReadModels;

public sealed class ProjectsAssetsReadModel
{
	public required Directory Assets { get; init; }

	public sealed class Directory
	{
		public required Guid Id { get; init; }
		public required string Name { get; init; }
		public required DateTime CreatedAt { get; init; }
		public required DateTime? LastModifiedAt { get; init; }
		public required IEnumerable<Directory> Directories { get; init; }
		public required IEnumerable<File> Files { get; init; }
	}

	public sealed class File
	{
		public required Guid Id { get; init; }
		public required string Name { get; init; }
		public required uint SizeInBytes { get; init; }
		public required DateTime CreatedAt { get; init; }
		public required DateTime? LastModifiedAt { get; init; }
	}
}