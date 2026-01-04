using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Users.Core.Clients.IpApi;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record GoogleLogin(HttpContext HttpContext) : IHttpRequest;

internal sealed class GoogleLoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GoogleLogin, GoogleLoginHandler>("google-login")
			.ProducesError(400,
				$"`{nameof(Shared.Endpoints.SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.");
}

internal sealed class GoogleLoginHandler() : IHttpRequestHandler<GoogleLogin>
{
	public async Task<IResult> Handle(GoogleLogin request, CancellationToken cancellationToken)
	{
		var context = request.HttpContext;
		await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "https://localhost:8081/users/google-user" });
		return Results.Empty;
	}
}