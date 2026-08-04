using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shop.Core.Database;
using Shop.Core.ReadModels;
using Shop.Core.Services;

namespace Shop.Core.Features;

internal sealed record CreateSnapshot(
	[FromBody] CreateSnapshot.Data Body,
	CurrentUser CurrentUser,
	[FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(Guid GameId, string Version);
};

internal sealed class CreateSnapshotEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<CreateSnapshot, CreateSnapshotHandler>("{ProjectId}/create-snapshot")
		.RequireAuthorization()
		.Produces<GameVersionReadModel>(201)
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class CreateSnapshotHandler(
	ShopDbContext dbContext,
	GameSnapshotService snapshotService
	) : IHttpRequestHandler<CreateSnapshot>
{
	public async Task<IResult> Handle(CreateSnapshot request, CancellationToken cancellationToken)
	{
		var (gameId, version) = request.Body;
		var projectId = request.ProjectId;

		var gameVersion = await snapshotService.CreateSnapshot(gameId, projectId, version, cancellationToken);

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Created($"shop/game-versions/{gameVersion.Id}", GameVersionReadModel.From(gameVersion));
	}
}