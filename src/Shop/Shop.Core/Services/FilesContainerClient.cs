using Azure.Storage.Blobs;

namespace Shop.Core.Services;

internal sealed class FilesContainerClient(BlobServiceClient blobServiceClient)
{
	public const string ContainerName = "project-assets-files";

	public BlobContainerClient Container { get; } = blobServiceClient.GetBlobContainerClient(ContainerName);
}