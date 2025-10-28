using System.ComponentModel.DataAnnotations;

namespace Projects.Core.Entities;

enum AssetType { Folder = 0, File = 1 }
abstract class Asset
{
	public required Guid Id { get; init; }
	public required string Name { get; set; }
	public required AssetType Type { get; set; }
	public required Guid? ParentId { get; set; }
	bool IsRoot => ParentId == null;
	public required string BlobName { get; set; }
	// temporary commented, because it is never used
	// public readonly string BlobContainerName = "project-assets";
	public required Guid ProjectId { get; set; }
}

sealed class AssetFolder : Asset { }

sealed class AssetFile : Asset
{
	public required string Extension { get; set; }
	public required string ContentType { get; set; }
}