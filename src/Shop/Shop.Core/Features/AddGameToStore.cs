using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;
using Shop.Core.Entities.Funds;
using Shop.Core.Services;

namespace Shop.Core.Features;

internal sealed record AddGameToStore(
	[FromBody] AddGameToStore.Data Body,
	CurrentUser CurrentUser,
	[FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Title, string Description, string Version, decimal Amount, int AgeRestriction, GameStates State);
};

internal sealed class AddGameToStoreEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddGameToStore, AddGameToStoreHandler>("{ProjectId}/add-game-to-store")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");

}

internal sealed class AddGameToStoreHandler(
	ShopDbContext dbContext,
	BlobServiceClient blobServiceClient,
	FilesContainerClient filesContainerClient
	) : IHttpRequestHandler<AddGameToStore>
{
	public async Task<IResult> Handle(AddGameToStore request, CancellationToken cancellationToken)
	{
		var (title, description, version, amount, ageRestriction, state) = request.Body;
		var projectId = request.ProjectId;
		var user = request.CurrentUser;
		var game = Game.Create(title, description, amount, "PLN", ageRestriction, state, user.Id);
		await dbContext.Games.AddAsync(game, cancellationToken);

		var gameVersion = new GameVersion()
		{
			Id = Guid.NewGuid(),
			GameId = game.Id,
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

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Ok("Added game to store");
	}
}