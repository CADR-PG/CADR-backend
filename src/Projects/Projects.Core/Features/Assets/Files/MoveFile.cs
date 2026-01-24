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

namespace Projects.Core.Features.Assets.Files;

internal sealed record MoveFile([FromRoute] Guid ProjectId, [FromRoute] Guid FileId, [FromBody] MoveFile.Data Body) : IHttpRequest
{
	internal record Data(Guid TargetDirectoryId);
}

internal sealed class MoveFileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<MoveFile, MoveFileHandler>("{ProjectId}/assets/file/{FileId}/move")
		.Produces(StatusCodes.Status204NoContent)
		.AddValidation<MoveFile.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`ProjectAssetsFileNotFound`")
		.ProducesError(400, "`DirectoryNotFound`");
}

// TODO: dodanie walidacji czy folder istnieje
internal sealed class MoveFileHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<MoveFile>
{
	public async Task<IResult> Handle(MoveFile request, CancellationToken cancellationToken)
	{
		var (projectId, fileId, body) = request;

		if (await dbContext.AssetsDirectories.AnyAsync(x => x.ProjectId != projectId && x.DirectoryId == body.TargetDirectoryId, cancellationToken))
			return new ErrorResult("DirectoryNotFound", "Target directory does not exist.");

		var file = await dbContext.AssetsFiles.FirstOrDefaultAsync(af => af.ProjectId == projectId && af.Id == fileId, cancellationToken);

		if (file is null or { DirectoryId: null })
			return new ErrorResult("ProjectAssetsFileNotFound", "Project assets file does not exists", 404);

		file.DirectoryId = body.TargetDirectoryId;
		file.LastModifiedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class MoveFileValidator : AbstractValidator<MoveFile.Data>
{
	public MoveFileValidator()
	{
		RuleFor(x => x.TargetDirectoryId).NotEmpty();
	}
}