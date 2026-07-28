using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shop.Core.Database;

namespace Shop.Core.Features;

internal sealed record AddGameToStore(
	[FromBody] AddGameToStore.Data Body,
	CurrentUser CurrentUser,
	[FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(Guid GameId, string Version);
};

internal sealed class AddGameToStoreEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddGameToStore, AddGameToStoreHandler>("add-game-to-store")
		.AddValidation<AddGameToStore>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");

}

internal sealed class AddGameToStoreHandler() : IHttpRequestHandler<AddGameToStore>
{
	public Task<IResult> Handle(AddGameToStore request, CancellationToken cancellationToken)
	{
		return new Task<IResult>(() => Results.Ok());
	}
}