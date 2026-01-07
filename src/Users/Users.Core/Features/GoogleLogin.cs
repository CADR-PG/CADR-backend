using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.Settings;
using System.Text.Json;
using Users.Core.Clients.IpApi;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;
using Users.Core.Services;
using Users.Core.Settings;

namespace Users.Core.Features;

internal record GoogleLogin([FromBody] GoogleLogin.Credentials Body, HttpContext HttpContext) : IHttpRequest
{
	internal record Credentials(string code);
}

internal sealed class GoogleLoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<GoogleLogin, GoogleLoginHandler>("google-login")
			.ProducesError(400,
				$"`{nameof(Shared.Endpoints.SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.")
			.AddValidation<GoogleLogin.Credentials>();
}

internal sealed class GoogleLoginHandler(
	UsersDbContext dbContext,
	IConfiguration configuration
	) : IHttpRequestHandler<GoogleLogin>
{
	public async Task<IResult> Handle(GoogleLogin request, CancellationToken cancellationToken)
	{
		var googleSettings = configuration.GetSettings<GoogleClientSettings>();
		var values = new Dictionary<string, string>
		{
			{ "code", request.Body.code },
			{ "client_id", googleSettings.ClientId },
			{ "client_secret", googleSettings.ClientSecret },
			{ "redirect_uri", "postmessage" },
			{ "grant_type", "authorization_code"},
		};
		using var httpClient = new HttpClient();
		using var content = new FormUrlEncodedContent(values);
		var response = await httpClient.PostAsync(new Uri("https://oauth2.googleapis.com/token"), content, cancellationToken);
		if(!response.IsSuccessStatusCode) return Results.BadRequest(response.ReasonPhrase);

		var json = await response.Content.ReadAsStringAsync(cancellationToken);
		var token = JsonSerializer.Deserialize<GoogleTokensReadModel>(json);

		var payload = await GoogleJsonWebSignature.ValidateAsync(token!.IdToken);
		var users = await dbContext.Users.ToListAsync(cancellationToken);
		return Results.Ok(payload.Email);
	}
}

internal sealed class GoogleLoginValidator : AbstractValidator<GoogleLogin.Credentials>
{
	public GoogleLoginValidator()
	{
		RuleFor(x => x.code).NotEmpty();
	}
}