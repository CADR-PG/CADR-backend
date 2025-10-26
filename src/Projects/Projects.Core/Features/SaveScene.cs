using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Shared.Endpoints;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features;

internal sealed record SaveScene([FromRoute] Guid ProjectId, [FromBody] object Scene) : IHttpRequest
{

}

internal sealed class SaveSceneEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<SaveScene, SaveSceneHandler>("save-scene/{ProjectId}").RequireAuthorization().AddValidation<SaveScene>();
}

internal sealed class SaveSceneHandler(ProjectsDbContext dbContext) : IHttpRequestHandler<SaveScene>
{
	public async Task<IResult> Handle(SaveScene request, CancellationToken cancellationToken)
	{
		var project = await dbContext.Projects.Where(p => p.Id == request.ProjectId).FirstOrDefaultAsync(cancellationToken);

		if (project == null)
			return Results.NotFound();

		var serializedJson = request.Scene.ToString();

		project.JsonDocument = serializedJson;
		project.LastUpdate = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class SaveSceneValidator : AbstractValidator<SaveScene>
{
	public SaveSceneValidator()
	{
		RuleFor(p => p.ProjectId).NotEmpty();
		RuleFor(p => p.Scene).NotEmpty();
	}
}