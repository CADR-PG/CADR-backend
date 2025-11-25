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


internal sealed record CreateDirectory([FromBody] CreateDirectory.Data Body, CurrentUser CurrentUser, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Name, Guid? ParentId);
}

internal sealed class CreateDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateDirectory, CreateDirectoryHandler>("create-directory/{projectId}")
		.AddValidation<CreateFile.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateDirectoryHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<CreateDirectory>
{
	public async Task<IResult> Handle(CreateDirectory request, CancellationToken cancellationToken)
	{
		var (name, parent) = request.Body;
		var projectId = request.ProjectId;
		var id = Guid.NewGuid();
		var asset = new AssetDirectory
		{
			Id = id,
			Name = name,
			Type = AssetType.Directory,
			BlobPath = "",
			ParentId = parent,
			ProjectId = projectId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		await dbContext.Assets.AddAsync(asset, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Ok(new
		{
			AssetId = asset.Id,
			AssetName = asset.Name,
		});
	}
}

internal sealed class DirectoryValidator : AbstractValidator<CreateDirectory.Data>
{
	public DirectoryValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}