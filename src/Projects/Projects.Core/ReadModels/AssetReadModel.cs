using Projects.Core.Entities;
using System.Text.Json.Serialization;

namespace Projects.Core.ReadModels;

internal class DirectoryReadModel
{
	public Guid Id { get; init; }
	public string Name { get; init; } = null!;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<DirectoryReadModel>? Directories { get; init; } = new();

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<FileReadModel>? Files { get; init; } = new();
}

internal class FileReadModel
{
	public Guid Id { get; init; }
	public string Name { get; init; } = null!;
	public string Extension { get; init; } = null!;
	public DateTime CreatedAt { get; init; }
	public DateTime UpdatedAt { get; init; }
}