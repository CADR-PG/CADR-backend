using FluentValidation;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Flows;
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
	internal record Credentials(string Code);
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
	ITokenProvider tokenProvider,
	IIpApiClient ipApiClient,
	GoogleAuthorizationCodeFlow.Initializer FlowInitializer
	) : IHttpRequestHandler<GoogleLogin>
{
	public async Task<IResult> Handle(GoogleLogin request, CancellationToken cancellationToken)
	{
		using var flow = new GoogleAuthorizationCodeFlow(FlowInitializer);
		var token = await flow.ExchangeCodeForTokenAsync(
			userId: null,
			code: request.Body.Code,
			redirectUri: "postmessage",
			CancellationToken.None
		);

		var payload = await GoogleJsonWebSignature.ValidateAsync(token!.IdToken);
		var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == payload.Email, cancellationToken);
		if (user is null)
		{
			user = new User()
			{
				Id = Guid.NewGuid(),
				FirstName = payload.GivenName,
				LastName = payload.FamilyName,
				Email = payload.Email,
				HashedPassword = HashingService.Hash(Guid.NewGuid().ToString()),
				LastLoggedInAt = DateTime.UtcNow,
			};
			await dbContext.Users.AddAsync(user, cancellationToken);
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		var ipAddress = request.HttpContext.GetClientIpAddress();
		var ipAddressLocation = await ipApiClient.GetIpAddressGeolocationData(ipAddress);

		var tokens = tokenProvider.Generate(user);
		var refreshToken = request.HttpContext.GetRefreshToken();
		if (await tokenProvider.GetTokenIdentifiers(refreshToken) is { } tokenIdentifiers)
			user.Refresh(tokenIdentifiers.TokenId, tokens, ipAddressLocation);
		else
			user.Login(tokens, ipAddressLocation);

		await dbContext.SaveChangesAsync(cancellationToken);
		request.HttpContext.SetTokenCookies(tokens);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class GoogleLoginValidator : AbstractValidator<GoogleLogin.Credentials>
{
	public GoogleLoginValidator()
	{
		RuleFor(x => x.Code).NotEmpty();
	}
}