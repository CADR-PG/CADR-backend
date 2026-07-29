using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shop.Core.Database;
using Shop.Core.Entities.Catalog;
using Shop.Core.Entities.Funds;

namespace Shop.Core.Features;

internal sealed record AddGameToStore(
	[FromBody] AddGameToStore.Data Body,
	CurrentUser CurrentUser,
	[FromRoute] Guid ProjectId) : IHttpRequest
{
	internal record Data(string Title, string Description, string Version, decimal Amount, int AgeRestriction, GameStates State);
};

internal sealed class AddGameToStoreEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPost<AddGameToStore, AddGameToStoreHandler>("{ProjectId}/add-game-to-store")
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");

}

internal sealed class AddGameToStoreHandler() : IHttpRequestHandler<AddGameToStore>
{
	public Task<IResult> Handle(AddGameToStore request, CancellationToken cancellationToken)
	{
		var (title, description,version, amount, ageRestriction, state) = request.Body;
		var projectId = request.ProjectId;
		var user = request.CurrentUser;
		var game = Game.Create(title, description, amount, "PLN", ageRestriction, state, user.Id);
		return Task.FromResult<IResult>(Results.Ok(game));
	}
}