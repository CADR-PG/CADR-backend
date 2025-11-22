using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Entities;
using Projects.Core.ReadModels;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Projects;


internal sealed record AddProject([FromBody] AddProject.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	internal record Data(string Name, string Description);
}

internal sealed class AddProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddProject, AddProjectHandler>("add-project")
		.AddValidation<AddProject.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class AddProjectHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<AddProject>
{
	public async Task<IResult> Handle(AddProject request, CancellationToken cancellationToken)
	{
		var (name, description) = request.Body;
		var currentUser = request.CurrentUser;
		var exists = await dbContext.Projects.Where(p => p.UserId == currentUser.Id).AnyAsync(p => p.Name == name && p.Description == description, cancellationToken);
		if (exists)
			return Errors.ProjectWithThisNameAlreadyExists;
		var project = new Project
		{
			Id = Guid.NewGuid(),
			Name = name,
			Description = description,
			LastUpdate = DateTime.UtcNow,
			UserId = currentUser.Id,
		};

		await dbContext.Projects.AddAsync(project, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = ProjectReadModel.From(project);

		return Results.Ok(readModel);
	}
}

internal sealed class ProjectValidator : AbstractValidator<AddProject.Data>
{
	public ProjectValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}