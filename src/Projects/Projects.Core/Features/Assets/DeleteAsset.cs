using Azure.Storage.Blobs;
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


internal sealed record DeleteAsset([FromBody] DeleteAsset.Data Body, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(Guid AssetId);
}

internal sealed class DeleteAssetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapDelete<DeleteAsset, DeleteAssetHandler>("delete-asset/{projectId}")
		.AddValidation<DeleteAsset.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class DeleteAssetHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
	) : IHttpRequestHandler<DeleteAsset>
{
	public async Task<IResult> Handle(DeleteAsset request, CancellationToken cancellationToken)
	{
		var assetId = request.Body.AssetId;
		var projectId = request.ProjectId;
		var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

		if (asset is null) return
			Results.NotFound();

		if (asset is AssetFile file)
		{
			var blobPath = projectId + "/" + assetId + Path.GetExtension(file.Name);
			var blobClient = container.GetBlobClient(blobPath);
			await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
			dbContext.Assets.Remove(asset);
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		return Results.Ok();
	}
}

internal sealed class DeleteAssetValidator : AbstractValidator<DeleteAsset.Data>
{
	public DeleteAssetValidator()
	{
		RuleFor(x => x.AssetId).NotEmpty();
	}
}