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

internal sealed record DeleteProject([FromRoute] Guid ProjectId) : IHttpRequest
{

}

internal sealed class DeleteProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<DeleteProject, DeleteProjectHandler>("delete-project/{ProjectId}").RequireAuthorization().AddValidation<DeleteProject>();
}

internal sealed class DeleteProjectHandler(ProjectsDbContext dbContext) : IHttpRequestHandler<DeleteProject>
{
	public async Task<IResult> Handle(DeleteProject request, CancellationToken cancellationToken)
	{
		var project = await dbContext.Projects.Where(p => p.Id == request.ProjectId).FirstOrDefaultAsync(cancellationToken);

		if (project == null)
			return Results.NotFound();

		dbContext.Remove(project);

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class DeleteProjectValidator : AbstractValidator<DeleteProject>
{
	public DeleteProjectValidator()
	{
		RuleFor(p => p.ProjectId).NotEmpty();
	}
}