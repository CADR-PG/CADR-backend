using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Entities;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets;

internal sealed record ChangeAssetContent([FromBody] ChangeAssetContent.Data Body) : IHttpRequest
{
	internal record Data(Guid Id, string ContentType);
}

internal sealed class ChangeAssetContentEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<ChangeAssetContent, ChangeAssetContentHandler>("change-asset-content/{projectId}")
		.AddValidation<ChangeAssetContent.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`Unauthorize`");
}

internal sealed class ChangeAssetContentHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
	) : IHttpRequestHandler<ChangeAssetContent>
{
	public async Task<IResult> Handle(ChangeAssetContent request, CancellationToken cancellationToken)
	{
		var (assetId, contentType) = request.Body;
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
		if (asset == null)
			return Results.NotFound();


		if (asset is AssetFile file)
		{
			file.ContentType = contentType;
			await dbContext.SaveChangesAsync(cancellationToken);
			var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
			var blob = container.GetBlobClient(file.BlobPath);

			await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);

			var sas = new Azure.Storage.Sas.BlobSasBuilder
			{
				BlobContainerName = Asset.BlobContainerName,
				BlobName = file.BlobPath,
				Resource = "b",
				ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
			};

			sas.SetPermissions(
				Azure.Storage.Sas.BlobContainerSasPermissions.Read |
				Azure.Storage.Sas.BlobContainerSasPermissions.Write);
			var uploadUrl = blob.GenerateSasUri(sas).AbsoluteUri;

			return Results.Ok(new
			{
				AssetId = assetId,
				UploadUrl = uploadUrl
			});
		}

		return Results.BadRequest();
	}
}

internal sealed class ChangeAssetContentValidator : AbstractValidator<ChangeAssetContent.Data>
{
	public ChangeAssetContentValidator()
	{
		RuleFor(x => x.Id).NotEmpty();
		RuleFor(x => x.ContentType).NotEmpty();
	}
}