using Azure.Storage.Blobs;
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

namespace Projects.Core.Features.Assets;

internal sealed record RenameAsset([FromBody] RenameAsset.Data Body) : IHttpRequest
{
	internal record Data(Guid Id, string Name);
}

internal sealed class RenameAssetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<RenameAsset, RenameAssetHandler>("change-asset-name")
		.AddValidation<RenameAsset.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`Unauthorize`");
}

internal sealed class RenameAssetHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<RenameAsset>
{
	public async Task<IResult> Handle(RenameAsset request, CancellationToken cancellationToken)
	{
		var (assetId, assetName) = request.Body;
		var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

		if (asset == null)
			return Results.NotFound();

		asset.Name = assetName;
		asset.UpdatedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.NoContent();
	}
}

internal sealed class RenameAssetValidator : AbstractValidator<RenameAsset.Data>
{
	public RenameAssetValidator()
	{
		RuleFor(x => x.Id).NotEmpty();
		RuleFor(x => x.Name).NotEmpty();
	}
}