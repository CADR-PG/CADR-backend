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


internal sealed record CreateFile([FromBody] CreateFile.Data Body, CurrentUser CurrentUser, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Name, Guid? ParentId, string ContentType, double FileSize);
}

internal sealed class CreateFileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateFile, CreateFileHandler>("create-file/{projectId}")
		.AddValidation<CreateFile.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateFileHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
) : IHttpRequestHandler<CreateFile>
{
	public async Task<IResult> Handle(CreateFile request, CancellationToken cancellationToken)
	{
		var (name,  parent, contentType, fileSize) = request.Body;
		var projectId = request.ProjectId;
		var id = Guid.NewGuid();
		var asset = new AssetFile()
		{
			Id = id,
			Name = Path.GetFileName(name),
			Type = AssetType.File,
			ParentId = parent,
			BlobPath = projectId + "/" + id + Path.GetExtension(name),
			ProjectId = projectId,
			Extension = Path.GetExtension(name),
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			ContentType = contentType,
			FileSize = fileSize
		};

		await dbContext.Assets.AddAsync(asset, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		string? uploadUrl = null;
		var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
		var blobClient = container.GetBlobClient(asset.BlobPath);

		var sas = new Azure.Storage.Sas.BlobSasBuilder
		{
			BlobContainerName = container.Name,
			BlobName = asset.BlobPath,
			Resource = "b",
			ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(3)
		};
		sas.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Write | Azure.Storage.Sas.BlobSasPermissions.Create);
		uploadUrl = blobClient.GenerateSasUri(sas).AbsoluteUri;

		return Results.Ok(new
		{
			AssetId = asset.Id,
			UploadUrl = uploadUrl
		});
	}
}

internal sealed class FileValidator : AbstractValidator<CreateFile.Data>
{
	public FileValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}