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

internal sealed record MoveDirectory([FromRoute] Guid ProjectId, [FromRoute] Guid DirectoryId, [FromBody] MoveDirectory.Data Body) : IHttpRequest
{
	internal record Data(Guid TargetDirectoryId);
}

internal sealed class MoveDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<MoveDirectory, MoveDirectoryHandler>("{ProjectId}/assets/directories/{DirectoryId}/move")
		.Produces(StatusCodes.Status204NoContent)
		.AddValidation<MoveDirectory.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`DirectoryNotFound`, `ProjectAssetsDirectoryNotFound`, `InvalidTargetDictionary`");
}

internal sealed class MoveDirectoryHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<MoveDirectory>
{
	public async Task<IResult> Handle(MoveDirectory request, CancellationToken cancellationToken)
	{
		var (projectId, directoryId, body) = request;

		if (await dbContext.AssetsDirectories.AnyAsync(x => x.ProjectId != projectId && x.DirectoryId == body.TargetDirectoryId, cancellationToken))
			return new ErrorResult("DirectoryNotFound", "The target directory does not exist.");

		var directory = await dbContext.AssetsDirectories.FirstOrDefaultAsync(af => af.ProjectId == projectId && af.Id == directoryId, cancellationToken);

		if (directory is null or { DirectoryId: null })
			return new ErrorResult("ProjectAssetsDirectoryNotFound", "The directory to be moved does not exist.");

		if (directory.IsRoot)
			return new ErrorResult("InvalidTargetDictionary", "The root dictionary cannot be moved.");

		// TODO: disallow move dictionary to their subdictionaries !IMPORTANT

		directory.DirectoryId = body.TargetDirectoryId;
		directory.LastModifiedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class MoveDirectoryValidator : AbstractValidator<MoveDirectory.Data>
{
	public MoveDirectoryValidator()
	{
		RuleFor(x => x.TargetDirectoryId).NotEmpty();
	}
}