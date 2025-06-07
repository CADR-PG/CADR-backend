using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using System.Text.Json;
using Users.Core.Database;
using Users.Core.Entities;

namespace Users.Core.Features;


internal sealed record AddProject([FromBody] AddProject.Credentials Body, CurrentUser CurrentUser) : IHttpRequest
{
	internal record Credentials(string Name, string Description);
}

internal sealed class AddProjectEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddProject, AddProjectHandler>("add_project")
		.AddValidation<AddProject.Credentials>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class AddProjectHandler(
	UsersDbContext dbContext
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

		return Results.NoContent();
	}
}

internal sealed class ProjectValidator : AbstractValidator<AddProject.Credentials>
{
	public ProjectValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}