using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.ValueObjects;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;
using Shop.Core.ReadModels;

namespace Shop.Core.Features;

internal sealed record PlayGame(
	CurrentUser CurrentUser,
	[FromRoute] Guid GameId) : IHttpRequest;

internal sealed class PlayGameEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<PlayGame, PlayGameHandler>("{GameId}/play-game")
		.Produces<PlayGameReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GameVersionNotFound`");
}

internal sealed class PlayGameHandler(
	ShopDbContext dbContext,
	BlobServiceClient blobServiceClient,
	ILogger<PlayGameHandler> logger
) : IHttpRequestHandler<PlayGame>
{
	public async Task<IResult> Handle(PlayGame request, CancellationToken cancellationToken)
	{
		var gameId = request.GameId;
		var userId = request.CurrentUser.Id;

		var isGameInUsersLibrary = await dbContext.LibraryEntries.AnyAsync(x => x.UserId == userId && x.GameId == gameId, cancellationToken);

		if (!isGameInUsersLibrary)
			return Results.Unauthorized();

		var version = await dbContext.Games
			.Where(g => g.Id == gameId && g.ActiveVersionId != null)
			.Join(dbContext.GameVersions, g => g.ActiveVersionId, v => v.Id, (g, v) => v)
			.FirstOrDefaultAsync(cancellationToken);

		if (version is null)
			return new ErrorResult("GameVersionNotFound", "Game has no published version.", 404);

		await RegisterPlay(gameId, userId, cancellationToken);

		var container = blobServiceClient.GetBlobContainerClient(GameVersion.BlobContainerName);
		var prefix = $"{version.BlobResourceName}/{version.ProjectId}/";
		var expiresOn = DateTimeOffset.UtcNow.AddHours(2);

		var assets = new List<GameAssetsReadModel>();
		await foreach (var blob in container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
		{
			var blobClient = container.GetBlobClient(blob.Name);
			var url = blobClient.GenerateSasUri(BlobSasPermissions.Read, expiresOn);
			assets.Add(new GameAssetsReadModel { Path = blob.Name[prefix.Length..], Url = url });
		}

		return Results.Ok(new PlayGameReadModel
		{
			GameId = gameId,
			VersionId = version.Id,
			ExpiresOn = expiresOn,
			Assets = assets
		});
	}
	private async Task RegisterPlay(Guid gameId, UserId userId, CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;

		try
		{
			var updated = await dbContext.GamePlays
				.Where(i => i.GameId == gameId && i.UserId == userId)
				.ExecuteUpdateAsync(s => s.SetProperty(i => i.LastPlayedAt, now), cancellationToken);

			if (updated > 0)
				return;

			var play = GamePlays.Create(gameId, userId, now, now);
			dbContext.GamePlays.Add(play);

			try
			{
				await dbContext.SaveChangesAsync(cancellationToken);
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
			{
				dbContext.Entry(play).State = EntityState.Detached;
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			logger.LogWarning(ex, "Failed to register play of game {GameId} for user {UserId}", gameId, userId);
		}
	}
}