using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using System.Text.Json;
using Users.Core.Database;

namespace Users.Core.Features;

internal sealed record LoadScene([FromRoute] Guid ProjectId) : IHttpRequest;

internal sealed class LoadSceneEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints.MapGet<LoadScene, LoadSceneHandler>("load_scene/{ProjectId}")
		.RequireAuthorization();
}

internal sealed class LoadSceneHandler(
	UsersDbContext dbContext
	) : IHttpRequestHandler<LoadScene>
{
	public async Task<IResult> Handle(LoadScene request, CancellationToken cancellationToken)
	{
		var projectId = request.ProjectId;
		var project = await dbContext.Projects.Where(p => p.Id == projectId).FirstAsync(cancellationToken);
		var sceneString = project.JsonDocument;

		if (string.IsNullOrEmpty(sceneString))
			return Results.BadRequest();

		var sceneJson = JsonSerializer.Deserialize<JsonDocument>(sceneString);

		return Results.Ok(sceneJson);
	}
}