using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Validation;
using Users.Core.Database;
using Users.Core.Entities;

namespace Users.Core.Features;

internal sealed record ModifyProject([FromBody] ModifyProject.Credentials Body) : IHttpRequest
{
	internal sealed record Credentials(Guid ProjectId, string Name, string Description);
}

internal sealed class ModifyProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPut<ModifyProject, ModifyProjectHandler>("modify_project")
			.RequireAuthorization().AddValidation<ModifyProject.Credentials>();
}

internal sealed class ModifyProjectHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ModifyProject>
{
	public async Task<IResult> Handle(ModifyProject request, CancellationToken cancellationToken)
	{
		var (id, name, description) = request.Body;
		var project = await dbContext.Projects.Where(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
		if (project == null)
			return Results.NotFound();

		project.Name = name;
		project.Description = description;
		project.LastUpdate = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Ok();
	}
}

internal sealed class ModifyProjectValidator : AbstractValidator<ModifyProject.Credentials>
{
	public ModifyProjectValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}