using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Shared.Endpoints;
using Shared.Endpoints.Results;

namespace Projects.Core.Features.Assets.Directories;


internal sealed record DeleteDirectory([FromRoute] Guid ProjectId, [FromRoute] Guid DirectoryId) : IHttpRequest;

internal sealed class DeleteDirectoryEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapDelete<DeleteDirectory, DeleteDirectoryHandler>("{ProjectId}/assets/directories/{DirectoryId}")
		.Produces(StatusCodes.Status204NoContent)
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`ProjectAssetsDirectoryNotFound`");
}

// TODO: trzeba usuwanie przenieść do workera w pamięci np HttpTrigger:
// zgodnie z konfiguracją ef core pliki mają ustawiane null na directoryId, jak folder i jego subfoldery są usuwane
// worker może zbierać te z directoryId np. co 5 minut i usuwać z bloba
// zapewni to lepszy UX bo query to jedno query do bazy a nie rekurencyjne calle do bazy
// PS. baza sama kaskadowo usunie subdirectories dla directory
// TODO: błąd dla roota??
internal sealed class DeleteDirectoryHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<DeleteDirectory>
{
	public async Task<IResult> Handle(DeleteDirectory request, CancellationToken cancellationToken)
	{
		var (projectId, directoryId) = request;

		var deletedCount = await dbContext.AssetsDirectories
			.Where(x => x.ProjectId == projectId && x.Id == directoryId)
			.ExecuteDeleteAsync(cancellationToken);

		return deletedCount == 1
			? Results.NoContent()
			: new ErrorResult("ProjectAssetsDirectoryNotFound", "Project assets directory does not exist");
	}
}