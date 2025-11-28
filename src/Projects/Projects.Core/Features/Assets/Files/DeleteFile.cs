using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Shared.Endpoints;
using Shared.Endpoints.Results;

namespace Projects.Core.Features.Assets.Files;


internal sealed record DeleteFile([FromRoute] Guid ProjectId, [FromRoute] Guid FileId) : IHttpRequest;

internal sealed class DeleteFileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapDelete<DeleteFile, DeleteFileHandler>("{ProjectId}/assets/files/{FileId}")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`ProjectAssetsFileNotFound`");
}

// TODO: zmiany zwiazane z DeleteDirectory.cs
internal sealed class DeleteFileHandler(
	ProjectsDbContext dbContext
	) : IHttpRequestHandler<DeleteFile>
{
	public async Task<IResult> Handle(DeleteFile request, CancellationToken cancellationToken)
	{
		var (projectId, fileId) = request;

		var deletedCount = await dbContext.AssetsFiles
			.Where(x => x.ProjectId == projectId && x.DirectoryId == fileId)
			.ExecuteUpdateAsync(stc => stc.SetProperty(af => af.DirectoryId, null as Guid?), cancellationToken);

		return deletedCount == 0
			? Results.NoContent()
			: new ErrorResult("ProjectAssetsFileNotFound", "Project assets file does not exists", 404);
	}
}