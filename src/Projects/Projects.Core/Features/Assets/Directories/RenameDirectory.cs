using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets.Directories;

internal sealed record RenameDirectory([FromRoute] Guid ProjectId, [FromRoute] Guid DirectoryId, [FromBody] RenameDirectory.Data Body) : IHttpRequest
{
	internal record Data(string Name);
}

internal sealed class RenameDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RenameDirectory, RenameDirectoryHandler>("{ProjectId}/assets/directories/{DirectoryId}/rename")
		.Produces(204)
		.AddValidation<RenameDirectory.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`ProjectAssetsDirectoryNotFound`");
}

// TODO: błąd dla roota??
internal sealed class RenameDirectoryHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<RenameDirectory>
{
	public async Task<IResult> Handle(RenameDirectory request, CancellationToken cancellationToken)
	{
		var (projectId, directoryId, body) = request;

		var directory = await dbContext.AssetsDirectories.FirstOrDefaultAsync(af => af.ProjectId == projectId && af.Id == directoryId, cancellationToken);

		if (directory is null)
			return new ErrorResult("ProjectAssetsDirectoryNotFound", "Project assets directory not found", 404);

		directory.Name = body.Name;
		directory.LastModifiedAt = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class RenameDirectoryValidator : AbstractValidator<RenameDirectory.Data>
{
	public RenameDirectoryValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}