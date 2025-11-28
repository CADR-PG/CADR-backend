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

internal sealed record RenameFile([FromRoute] Guid ProjectId, [FromRoute] Guid FileId, [FromBody] RenameFile.Data Body) : IHttpRequest
{
	internal record Data(string Name);
}

internal sealed class RenameFileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RenameFile, RenameFileHandler>("{ProjectId}/assets/files/{FileId}/rename")
		.Produces(StatusCodes.Status204NoContent)
		.AddValidation<RenameFile.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`Unauthorize`")
		.ProducesError(404, "`ProjectAssetsFileNotFound`");
}

internal sealed class RenameFileHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<RenameFile>
{
	public async Task<IResult> Handle(RenameFile request, CancellationToken cancellationToken)
	{
		var (projectId, fileId, body) = request;

		var file = await dbContext.AssetsFiles.FirstOrDefaultAsync(af => af.ProjectId == projectId && af.Id == fileId, cancellationToken);

		if (file is null)
			return new ErrorResult("ProjectAssetsFileNotFound", "Project assets file does not exists", 404);

		file.Name = body.Name;
		file.LastModifiedAt = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class RenameFileValidator : AbstractValidator<RenameFile.Data>
{
	public RenameFileValidator()
	{
		RuleFor(x => x.Name).NotEmpty();
	}
}