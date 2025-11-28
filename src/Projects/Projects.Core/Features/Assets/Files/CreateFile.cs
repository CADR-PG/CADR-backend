using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Projects.Core.Database;
using Projects.Core.Entities;
using Projects.Core.ReadModels;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets.Files;

internal sealed record CreateFile([FromBody] CreateFile.Data Body, CurrentUser CurrentUser, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Name, Guid DirectoryId, uint SizeInBytes);
}

internal sealed class CreateFileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateFile, CreateFileHandler>("{ProjectId}/assets/files")
		.Produces<AssetsFileUploadReadModel>(201)
		.AddValidation<CreateFile.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

// TODO: dodanie lepszej walidacji, np. czy istnieje projekt/folder, czy nie ma już takiego pliku o tej nazwie...
internal sealed class CreateFileHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
) : IHttpRequestHandler<CreateFile>
{
	public async Task<IResult> Handle(CreateFile request, CancellationToken cancellationToken)
	{
		var (name, directoryId, sizeInBytes) = request.Body;
		var projectId = request.ProjectId;

		var file = AssetsFile.Create(projectId, directoryId, name, sizeInBytes);

		await dbContext.AssetsFiles.AddAsync(file, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var container = blobServiceClient.GetBlobContainerClient(AssetsFile.BlobContainerName);
		var blobClient = container.GetBlobClient(file.BlobResourceName);

		var uploadSas = new Azure.Storage.Sas.BlobSasBuilder
		{
			BlobContainerName = container.Name,
			BlobName = file.BlobResourceName,
			Resource = "b",
			ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(3)
		};
		uploadSas.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Write);

		var readModel = AssetsFileUploadReadModel.From(file, blobClient.GenerateSasUri(uploadSas));

		return Results.Created($"projects/{projectId}/assets/files/{file.Id}", readModel);
	}
}

internal sealed class FileValidator : AbstractValidator<CreateFile.Data>
{
	public FileValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}