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
using System.Runtime.InteropServices.JavaScript;

namespace Projects.Core.Features.Assets;

internal sealed record GetProjectAssets([FromRoute] Guid ProjectId) : IHttpRequest;

internal sealed class GetProjectAssetsEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetProjectAssets, GetAssetsTreeHandler>("{ProjectId}/assets")
		.Produces<ProjectsAssetsReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`ProjectNotFound`");
}

internal sealed class GetAssetsTreeHandler(
	ProjectsDbContext dbContext
	) : IHttpRequestHandler<GetProjectAssets>
{
	public async Task<IResult> Handle(GetProjectAssets request, CancellationToken cancellationToken)
	{
		var projectId = request.ProjectId;

		var assets = await dbContext.AssetsDirectories
			.Include(x => x.Files)
			.Where(a => a.ProjectId == projectId)
			.ToListAsync(cancellationToken);

		if (assets.Count == 0)
			return new ErrorResult("ProjectNotFound", "Project does not exist", 404);

		var readModel = new ProjectsAssetsReadModel { Assets = BuildDirectoryReadModel(assets.First(x => x.IsRoot)) };

		return Results.Ok(readModel);

		ProjectsAssetsReadModel.Directory BuildDirectoryReadModel(AssetsDirectory directory)
		{
			return new ProjectsAssetsReadModel.Directory
			{
				Id = directory.Id,
				Name = directory.Name,
				CreatedAt = directory.CreatedAt,
				LastModifiedAt = directory.LastModifiedAt,
				Directories = assets.Where(ad => ad.DirectoryId == directory.Id).Select(BuildDirectoryReadModel),
				Files = directory.Files.Select(af => new ProjectsAssetsReadModel.File
				{
					Id = af.Id,
					Name = af.Name,
					SizeInBytes = af.SizeInBytes,
					CreatedAt = af.CreatedAt,
					LastModifiedAt = af.LastModifiedAt,
				})
			};
		}
	}
}