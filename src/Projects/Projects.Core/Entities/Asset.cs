using System.ComponentModel.DataAnnotations;

namespace Projects.Core.Entities;

enum AssetType { Directory = 0, File = 1 }
abstract class Asset
{
	public required Guid Id { get; init; }
	public required string Name { get; set; }
	public required AssetType Type { get; set; }
	public Guid? ParentId { get; set; }
	public required string BlobPath { get; set; }
	public const string BlobContainerName = "project-assets";
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public required Guid ProjectId { get; set; }
	public Project? Project { get; init; }
}

sealed class AssetDirectory : Asset
{
	public bool IsRoot => ParentId == null;
}

sealed class AssetFile : Asset
{
	public required string Extension { get; set; }
	public required string ContentType { get; set; }
}