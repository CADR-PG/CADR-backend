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

namespace Projects.Core.Features.Assets;

internal sealed record GetAssetsTree([FromRoute] Guid ProjectId) : IHttpRequest;

internal sealed class GetAssetsTreeEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetAssetsTree, GetAssetsTreeHandler>("assets-tree/{projectId}")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetAssetsTreeHandler(
	ProjectsDbContext dbContext
	) : IHttpRequestHandler<GetAssetsTree>
{
	async public Task<IResult> Handle(GetAssetsTree request, CancellationToken cancellationToken)
	{
		var projectId = request.ProjectId;
		var assets = await dbContext.Assets.Where(a => a.ProjectId == projectId).ToListAsync(cancellationToken);

		List<DirectoryReadModel> BuildDirectories(Guid? parentId)
		{
			var files = BuildRootFiles(parentId);
			return assets
				.Where(a => a.ParentId == parentId && a.Type == AssetType.Directory)
				.Select(a => new DirectoryReadModel
				{
					Id = a.Id,
					Name = a.Name,
					Directories = BuildDirectories(a.Id),
					Files = assets
						.Where(f => f.Type == AssetType.File && f.ParentId == a.Id)
						.Select(f => new FileReadModel
						{
							Id = f.Id,
							Name = Path.GetFileName(f.Name),
							Extension = Path.GetExtension(f.Name),
							CreatedAt = f.CreatedAt,
							UpdatedAt = f.UpdatedAt
						})
						.ToList()
				})
				.ToList();
		}

		List<FileReadModel> BuildRootFiles(Guid? parentId)
		{
			return assets
				.Where(a => a.ParentId == parentId && a.Type == AssetType.File)
				.Select(a => new FileReadModel
				{
					Id = a.Id,
					Name = Path.GetFileName(a.Name),
					Extension = Path.GetExtension(a.Name),
					CreatedAt = a.CreatedAt,
					UpdatedAt = a.UpdatedAt
				})
				.ToList();
		}

		var tree = BuildDirectories(null);

		return Results.Ok(tree);
	}
}