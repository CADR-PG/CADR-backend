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

namespace Projects.Core.Features.Assets.Directories;

internal sealed record CreateDirectory([FromRoute] Guid ProjectId, CurrentUser CurrentUser, [FromBody] CreateDirectory.Data Body) : IHttpRequest
{
	internal record Data(string Name, Guid DirectoryId);
}

internal sealed class CreateDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateDirectory, CreateDirectoryHandler>("{ProjectId}/assets/directories")
		.Produces<AssetsDirectoryReadModel>()
		.AddValidation<CreateDirectory.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(400, "`ProjectNotFound`")
		.ProducesError(404, "`ProjectAssetsDirectoryNotFound`")
		.ProducesError(409, "`DictionaryNameConflict`");
}

internal sealed class CreateDirectoryHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<CreateDirectory>
{
	public async Task<IResult> Handle(CreateDirectory request, CancellationToken cancellationToken)
	{
		var (name, parentDirectoryId) = request.Body;

		if (!await dbContext.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken))
			return new ErrorResult("ProjectNotFound", "Project does not exist");

		if (!await dbContext.AssetsDirectories.AnyAsync(x => x.Id == parentDirectoryId, cancellationToken))
			return new ErrorResult("ProjectAssetsDirectoryNotFound", "Project assets directory does not exist");

		if (await dbContext.AssetsFiles.AnyAsync(x => x.ProjectId == request.ProjectId && x.DirectoryId == parentDirectoryId && x.Name == name, cancellationToken))
			return new ErrorResult("DictionaryNameConflict", "Dictionary with the same name already exists.", 409);

		var directory = AssetsDirectory.Create(request.ProjectId, name, parentDirectoryId);

		await dbContext.AssetsDirectories.AddAsync(directory, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = AssetsDirectoryReadModel.From(directory);
		return Results.Ok(readModel);
	}
}

internal sealed class DirectoryValidator : AbstractValidator<CreateDirectory.Data>
{
	public DirectoryValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}