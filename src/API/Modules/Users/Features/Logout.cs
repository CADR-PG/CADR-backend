using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules.Users.Features;

internal record Logout() : IHttpRequest
{

};

internal record LogoutReadModel(string response);

internal sealed class LogoutEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapDelete<Logout, LogoutHandler>("/logout");
}

internal sealed class LogoutHandler(
	IHttpContextAccessor httpContextAccessor
) : IHttpRequestHandler<Logout>
{
	public async Task<IResult> Handle(Logout request, CancellationToken cancellationToken)
{
	await Task.CompletedTask;
	httpContextAccessor.HttpContext!.Response.Cookies.Delete("jwt");
	httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken");
	return Results.Ok(new LogoutReadModel("Logged out successfully"));
}
}