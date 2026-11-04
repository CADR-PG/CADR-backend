using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.ReadModels;
using Shared.Endpoints;
using System.Text.Json;

namespace Projects.Core.Features.Projects;

internal sealed record LoadProject([FromRoute] Guid ProjectId) : IHttpRequest;

internal sealed class LoadProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints.MapGet<LoadProject, LoadProjectHandler>("load-project/{ProjectId}")
		.RequireAuthorization();
}

internal sealed class LoadProjectHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<LoadProject>
{
	public async Task<IResult> Handle(LoadProject request, CancellationToken cancellationToken)
	{
		var projectId = request.ProjectId;
		var project = await dbContext.Projects.Where(p => p.Id == projectId).FirstAsync(cancellationToken);
		var sceneString = project.EditorMetaData;

		if (string.IsNullOrEmpty(sceneString))
			return Results.BadRequest();

		// PRM zawiera project.Id, project.Name, project.Description, project.MainSceneFileId,
		//		project.EditorMetaData, project.LastUpdate
		var projectReadModel = ProjectReadModel.From(project);

		return Results.Ok(projectReadModel);
	}
}