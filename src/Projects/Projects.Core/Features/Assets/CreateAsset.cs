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
using Projects.Core.ReadModels;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets;


internal sealed record CreateAsset([FromBody] CreateAsset.Data Body, CurrentUser CurrentUser, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Name, AssetType Type, Guid? ParentId, string ContentType);
}

internal sealed class CreateAssetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateAsset, CreateAssetHandler>("create-asset/{projectId}")
		.AddValidation<CreateAsset.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateAssetHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
) : IHttpRequestHandler<CreateAsset>
{
	public async Task<IResult> Handle(CreateAsset request, CancellationToken cancellationToken)
	{
		var (name, type, parent, contentType) = request.Body;
		var projectId = request.ProjectId;
		Asset asset;
		var id = Guid.NewGuid();
		switch (type)
		{
			case AssetType.Folder:
				asset = new AssetFolder
				{
					Id = id,
					Name = name,
					Type = type,
					ParentId = parent,
					BlobPath = projectId + "/" + id,
					ProjectId = projectId
				};
				break;
			case AssetType.File:
				asset = new AssetFile()
				{
					Id = id,
					Name = Path.GetFileName(name),
					Type = type,
					ParentId = parent,
					BlobPath = projectId + "/" + id + Path.GetExtension(name),
					ProjectId = projectId,
					Extension = Path.GetExtension(name),
					ContentType = contentType
				};
				break;
			default:
				return Results.BadRequest();
		}

		await dbContext.Assets.AddAsync(asset, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		string? uploadUrl = null;
		if (asset is AssetFile file)
		{
			var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
			var blobClient = container.GetBlobClient(file.BlobPath);

			var sas = new Azure.Storage.Sas.BlobSasBuilder
			{
				BlobContainerName = container.Name,
				BlobName = file.BlobPath,
				Resource = "b",
				ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
			};
			sas.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Write | Azure.Storage.Sas.BlobSasPermissions.Create);
			uploadUrl = blobClient.GenerateSasUri(sas).AbsoluteUri;
		}

		return Results.Ok(new
		{
			AssetId = asset.Id,
			UploadUrl = uploadUrl
		});
	}
}

internal sealed class AssetValidator : AbstractValidator<CreateAsset.Data>
{
	public AssetValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}