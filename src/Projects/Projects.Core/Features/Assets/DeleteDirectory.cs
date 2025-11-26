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


internal sealed record DeleteDirectory([FromBody] DeleteDirectory.Data Body, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(Guid AssetId);
}

internal sealed class DeleteDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapDelete<DeleteDirectory, DeleteDirectoryHandler>("delete-directory/{projectId}")
		.AddValidation<DeleteDirectory.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class DeleteDirectoryHandler(
	ProjectsDbContext dbContext,
	BlobServiceClient blobServiceClient
) : IHttpRequestHandler<DeleteDirectory>
{
	public async Task<IResult> Handle(DeleteDirectory request, CancellationToken cancellationToken)
	{
		var assetId = request.Body.AssetId;
		var projectId = request.ProjectId;
		var container = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

		if (asset is null)
			return Results.NotFound();

		if (asset is AssetDirectory directory)
		{
			await DeleteDirectoryRecursive(assetId, projectId, container, cancellationToken);
		}
		else if (asset is AssetFile file)
		{
			var blobClient = container.GetBlobClient(file.BlobPath);
			await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
		}

		dbContext.Assets.Remove(asset);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Ok();
	}

	private async Task DeleteDirectoryRecursive(Guid directoryId, Guid projectId, BlobContainerClient container, CancellationToken cancellationToken)
	{
		var children = await dbContext.Assets
		   .Where(a => a.ParentId == directoryId)
		   .ToListAsync(cancellationToken);

		foreach (var child in children)
		{
			if (child is AssetDirectory)
			{
				await DeleteDirectoryRecursive(child.Id, projectId, container, cancellationToken);
			}
			else if (child is AssetFile file)
			{
				var blobClient = container.GetBlobClient(file.BlobPath);
				await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
			}
			dbContext.Assets.Remove(child);
		}

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}

internal sealed class DeleteDirectoryValidator : AbstractValidator<DeleteDirectory.Data>
{
	public DeleteDirectoryValidator()
	{
		RuleFor(x => x.AssetId).NotEmpty();
	}
}