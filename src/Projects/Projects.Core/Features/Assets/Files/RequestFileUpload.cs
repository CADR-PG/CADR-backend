using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Entities;
using Projects.Core.ReadModels;
using Shared.Endpoints;
using Shared.Endpoints.Results;

namespace Projects.Core.Features.Assets.Files;

internal sealed record RequestFileUpload([FromRoute] Guid ProjectId, [FromRoute] Guid FileId) : IHttpRequest;

internal sealed class RequestUploadEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RequestFileUpload, RequestFileUploadHandler>("{ProjectId}/assets/file/{FileId}/move")
		.Produces<AssetsFileDownloadReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`Unauthorized`")
		.ProducesError(404, "`ProjectAssetsFileNotFound`");
}

internal sealed class RequestFileUploadHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
	) : IHttpRequestHandler<RequestFileUpload>
{
	public async Task<IResult> Handle(RequestFileUpload request, CancellationToken cancellationToken)
	{
		var (projectId, fileId) = request;

		var file = await dbContext.AssetsFiles.FirstOrDefaultAsync(af => af.ProjectId == projectId && af.Id == fileId, cancellationToken);

		if (file is null)
			return new ErrorResult("ProjectAssetsFileNotFound", "Project assets file does not exists", 404);

		var container = blobServiceClient.GetBlobContainerClient(AssetsFile.BlobContainerName);
		var blobClient = container.GetBlobClient(file.BlobResourceName);

		var expiresOn = DateTimeOffset.UtcNow.AddMinutes(3);

		var downloadSasBuilder = new BlobSasBuilder
		{
			BlobName = file.BlobResourceName,
			Resource = "b",
			ExpiresOn = expiresOn,
		};
		downloadSasBuilder.SetPermissions(BlobSasPermissions.Write);

		var readModel = AssetsFileDownloadReadModel.From(file, blobClient.GenerateSasUri(downloadSasBuilder));

		return Results.Ok(readModel);
	}
}