using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Features;

internal sealed record PublishVersion(
	[FromBody] PublishVersion.Data Body,
	CurrentUser CurrentUser,
	[FromRoute] Guid GameId) : IHttpRequest
{
	internal record Data(Guid GameVersionId);
};

internal sealed class PublishVersionEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<PublishVersion, PublishVersionHandler>("{GameId}/publish-version")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`GameVersionNotFound`");
}

internal sealed class PublishVersionHandler(
	ShopDbContext dbContext
	) : IHttpRequestHandler<PublishVersion>
{
	public async Task<IResult> Handle(PublishVersion request, CancellationToken cancellationToken)
	{
		var gameId = request.GameId;
		var gameVersionId = request.Body.GameVersionId;

		var versionBelongsToGame = await dbContext.GameVersions
			.AnyAsync(v => v.Id == gameVersionId && v.GameId == gameId, cancellationToken);
		if (!versionBelongsToGame)
			return new ErrorResult("GameVersionNotFound", "Version does not belong to this game.", 404);

		var game = await dbContext.Games.FirstAsync(g => g.Id == gameId, cancellationToken);

		game.ActiveVersionId = gameVersionId;
		game.State = GameStates.Published;

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.Ok();
	}
}