using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Entities;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets;

internal sealed record DownloadAsset([FromBody] DownloadAsset.Data Body) : IHttpRequest
{
	internal record Data(Guid AssetId);
}

internal sealed class DownloadAssetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<DownloadAsset, DownloadAssetHandler>("download-asset")
		.AddValidation<DownloadAsset.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class DownloadAssetHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
) : IHttpRequestHandler<DownloadAsset>
{
	public async Task<IResult> Handle(DownloadAsset request, CancellationToken cancellationToken)
	{
		var assetId = request.Body.AssetId;
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
		if (asset is null)
			return Results.NotFound();

		var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
		var blobClient = container.GetBlobClient(asset.BlobPath);

		var expiresOn = DateTimeOffset.UtcNow.AddMinutes(30);

		var sasBuilder = new BlobSasBuilder
		{
			BlobName = asset.BlobPath,
			Resource = "b",
			ExpiresOn = expiresOn,
		};

		sasBuilder.SetPermissions(BlobSasPermissions.Read);

		var sasUri = blobClient.GenerateSasUri(sasBuilder);

		return Results.Ok(sasUri.ToString());
	}
}

internal sealed class DownloadAssetValidator : AbstractValidator<DownloadAsset.Data>
{
	public DownloadAssetValidator()
	{
		RuleFor(a => a.AssetId).NotEmpty();
	}
}