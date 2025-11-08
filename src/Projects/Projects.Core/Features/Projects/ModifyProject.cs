using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.ReadModels;
using Shared.Endpoints;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Projects;

internal sealed record ModifyProject([FromRoute] Guid ProjectId, [FromBody] ModifyProject.Data Body) : IHttpRequest
{
	internal sealed record Data(string Name, string Description);
}

internal sealed class ModifyProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPut<ModifyProject, ModifyProjectHandler>("modify-project/{ProjectId}")
			.RequireAuthorization().AddValidation<ModifyProject.Data>();
}

internal sealed class ModifyProjectHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<ModifyProject>
{
	public async Task<IResult> Handle(ModifyProject request, CancellationToken cancellationToken)
	{
		var (name, description) = request.Body;
		var project = await dbContext.Projects.Where(p => p.Id == request.ProjectId).FirstOrDefaultAsync(cancellationToken);
		if (project == null)
			return Results.NotFound();

		project.Name = name;
		project.Description = description;
		project.LastUpdate = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
		var readModel = ProjectReadModel.From(project);

		return Results.Ok(readModel);
	}
}

internal sealed class ModifyProjectValidator : AbstractValidator<ModifyProject.Data>
{
	public ModifyProjectValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}