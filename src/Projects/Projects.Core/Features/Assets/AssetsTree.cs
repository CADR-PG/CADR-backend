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

internal sealed record AssetsTree([FromRoute] Guid ProjectId) : IHttpRequest;

internal sealed class AssetsTreeEndpoint() : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AssetsTree, AssetsTreeHandler>("assets-tree/{projectId}")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class AssetsTreeHandler(
	ProjectsDbContext dbContext
	) : IHttpRequestHandler<AssetsTree>
{
	async public Task<IResult> Handle(AssetsTree request, CancellationToken cancellationToken)
	{
		var projectId = request.ProjectId;
		var assets = await dbContext.Assets.Where(a => a.ProjectId == projectId).ToListAsync(cancellationToken);

		List<AssetDTO> Build(Guid? parentId)
		{
			return assets
				.Where(a => a.ParentId == parentId)
				.Select(a => new AssetDTO
				{
					Id = a.Id,
					Name = a.Name,
					Type = a.Type,
					Children = a.Type == AssetType.Folder
						? Build(a.Id)
						: null
				})
				.ToList();
		}

		var tree = Build(null);
		return Results.Ok(tree);
	}
}