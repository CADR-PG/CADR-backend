using API.Modules.Users.Services;
using API.Shared.Endpoints;

namespace API.Modules.Users.Features;

internal record Logout : IHttpRequest;

internal sealed class LogoutEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Logout, LogoutHandler>("/logout");
}

internal sealed class LogoutHandler(
	UserTokensHttpStorage userTokensHttpStorage
) : IHttpRequestHandler<Logout>
{
	public Task<IResult> Handle(Logout request, CancellationToken cancellationToken)
	{
		userTokensHttpStorage.Clear();
		return Task.FromResult(Results.NoContent());
	}
}