using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Shared.Endpoints;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Projects;

internal sealed record SetEditorMetaData([FromRoute] Guid ProjectId, [FromBody] object Scene, Guid MainFileSceneId) : IHttpRequest
{

}

internal sealed class SetEditorMetaDataEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<SetEditorMetaData, SetEditorMetaDataHandler>("save-scene/{ProjectId}").RequireAuthorization().AddValidation<SetEditorMetaData>();
}

internal sealed class SetEditorMetaDataHandler(ProjectsDbContext dbContext) : IHttpRequestHandler<SetEditorMetaData>
{
	public async Task<IResult> Handle(SetEditorMetaData request, CancellationToken cancellationToken)
	{
		var project = await dbContext.Projects.Where(p => p.Id == request.ProjectId).FirstOrDefaultAsync(cancellationToken);

		if (project == null)
			return Results.NotFound();

		var scene = request.Scene;

		project.EditorMetaData = scene.ToString();
		project.LastUpdate = DateTime.UtcNow;

		var mainSceneId = request.MainFileSceneId;

		if (project.MainSceneFileId == Guid.Empty &&  mainSceneId != Guid.Empty)
			project.MainSceneFileId = mainSceneId;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class SetEditorMetaDataValidator : AbstractValidator<SetEditorMetaData>
{
	public SetEditorMetaDataValidator()
	{
		RuleFor(p => p.ProjectId).NotEmpty();
		RuleFor(p => p.Scene).NotEmpty();
	}
}