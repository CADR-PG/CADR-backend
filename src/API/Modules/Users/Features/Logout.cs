﻿using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

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
		// Invalidate the JWT token by deleting the cookie
		httpContextAccessor.HttpContext!.Response.Cookies.Delete("jwt");
		return Results.Ok(new LogoutReadModel("Logged out successfully"));
	}
}