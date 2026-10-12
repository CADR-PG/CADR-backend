using Azure.Storage.Blobs;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Services;

internal sealed class GameSnapshotService(
	ShopDbContext dbContext,
	BlobServiceClient blobServiceClient,
	FilesContainerClient filesContainerClient)
{
	public async Task<GameVersion> CreateSnapshot(Guid gameId, Guid projectId, string version, CancellationToken cancellationToken)
	{
		var gameVersion = new GameVersion
		{
			Id = Guid.NewGuid(),
			GameId = gameId,
			ProjectId = projectId,
			Version = version,
			CreatedAt = DateTime.UtcNow,
		};
		await dbContext.GameVersions.AddAsync(gameVersion, cancellationToken);

		var sourceContainer = filesContainerClient.Container;
		var destinationContainer = blobServiceClient.GetBlobContainerClient(GameVersion.BlobContainerName);

		await foreach (var blobItem in sourceContainer.GetBlobsAsync(prefix: $"{projectId}/", cancellationToken: cancellationToken))
		{
			var source = sourceContainer.GetBlobClient(blobItem.Name);
			var destination = destinationContainer.GetBlobClient($"{gameVersion.BlobResourceName}/{blobItem.Name}");
			await destination.SyncCopyFromUriAsync(source.Uri, null, cancellationToken);
		}

		return gameVersion;
	}
}