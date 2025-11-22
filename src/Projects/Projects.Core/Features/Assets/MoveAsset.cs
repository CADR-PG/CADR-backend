using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Entities;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;

namespace Projects.Core.Features.Assets;

internal sealed record MoveAsset([FromBody] MoveAsset.Data Body, [FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(Guid? AssetId, Guid? TargetParentId);
}

internal sealed class MoveAssetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<MoveAsset, MoveAssetHandler>("move-asset/{projectId}")
		.AddValidation<MoveAsset.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class MoveAssetHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<MoveAsset>
{
	async public Task<IResult> Handle(MoveAsset request, CancellationToken cancellationToken)
	{
		var (assetId, targetParentId) = request.Body;
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

		if (asset == null)
			return Results.NotFound();

		asset.ParentId = targetParentId;
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok();
	}
}

internal sealed class MoveAssetValidator : AbstractValidator<MoveAsset.Data>
{
	public MoveAssetValidator()
	{
		RuleFor(x => x.AssetId).NotEmpty();
		RuleFor(x => x.TargetParentId).NotEmpty();
	}
}