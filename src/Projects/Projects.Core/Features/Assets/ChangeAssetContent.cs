// using Azure.Storage.Blobs;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.CodeAnalysis;
// using Projects.Core.Database;
// using Shared.Endpoints;
// using Shared.Endpoints.Results;
// using Shared.Endpoints.Validation;
//
// namespace Projects.Core.Features.Assets;
//
// internal sealed record ChangeAssetContent([FromBody] ChangeAssetContent.Data Body, [FromRoute] Guid ProjectId) : IHttpRequest
// {
// 	internal record Data(Guid Id);
// }
//
// internal sealed class ChangeAssetContentEndpoint : IEndpoint
// {
// 	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
// 		.MapPost<ChangeAssetContent, ChangeAssetContentHandler>("change-asset-content/{projectId}")
// 		.AddValidation<ChangeAssetContent.Data>()
// 		.RequireAuthorization()
// 		.ProducesError(401, "`Unauthorize`");
// }
//
// internal sealed class ChangeAssetContentHandler(
// 	ProjectsDbContext dbContext,
// 	BlobServiceClient blobServiceClient
// 	) : IHttpRequestHandler<ChangeAssetContent>
// {
// 	public async Task<IResult> Handle(ChangeAssetContent request, CancellationToken cancellationToken)
// 	{
//
// 	}
// }