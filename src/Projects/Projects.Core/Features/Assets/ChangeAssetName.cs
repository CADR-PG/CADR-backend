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

internal sealed record ChangeAssetName([FromBody] ChangeAssetName.Data Body) : IHttpRequest
{
	internal record Data(Guid Id, string Name);
}

internal sealed class ChangeAssetNameEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<ChangeAssetName, ChangeAssetNameHandler>("change-asset-name")
		.AddValidation<ChangeAssetName.Data>()
		.RequireAuthorization()
		.ProducesError(401, "`Unauthorize`");
}

internal sealed class ChangeAssetNameHandler(
	ProjectsDbContext dbContext
) : IHttpRequestHandler<ChangeAssetName>
{
	public async Task<IResult> Handle(ChangeAssetName request, CancellationToken cancellationToken)
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

internal sealed class ChangeAssetNameValidator : AbstractValidator<ChangeAssetName.Data>
{
	public ChangeAssetNameValidator()
	{
		RuleFor(x => x.Id).NotEmpty();
		RuleFor(x => x.Name).NotEmpty();
	}
}