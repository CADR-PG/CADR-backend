using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using System.Security.Claims;
using Users.Core.Clients.IpApi;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record GoogleCallback(HttpContext HttpContext) : IHttpRequest;

internal sealed class GoogleCallbackEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GoogleCallback, GoogleCallbackHandler>("google-user")
			.ProducesError(400,
				$"`{nameof(Shared.Endpoints.SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.");
}

internal sealed class GoogleCallbackHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider,
	IIpApiClient ipApiClient
	) : IHttpRequestHandler<GoogleCallback>
{
	public async Task<IResult> Handle(GoogleCallback request, CancellationToken cancellationToken)
	{
		var context = request.HttpContext;
		var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

		if (!result.Succeeded)
		{
			return Results.Unauthorized();
		}

		var claims = result.Principal!.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
		var email = result.Principal.FindFirstValue(ClaimTypes.Email)!;
		var firstName = result.Principal.FindFirstValue(ClaimTypes.GivenName);
		var lastName = result.Principal.FindFirstValue(ClaimTypes.Surname);

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.Include(x => x.UserLocationLogs)
			.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

		var ipAddress = request.HttpContext.GetClientIpAddress();
		var ipAddressLocation = await ipApiClient.GetIpAddressGeolocationData(ipAddress);

		if (user is not null)
		{
			if (await tokenProvider.GetTokenIdentifiers(request.HttpContext.GetRefreshToken()) is { } tokenIdentifiers)
				user.Refresh(tokenIdentifiers.TokenId, tokenProvider.Generate(user), ipAddressLocation);
			else
				user.Login(tokenProvider.Generate(user), ipAddressLocation);

			request.HttpContext.SetTokenCookies(tokenProvider.Generate(user));
		}
		else
		{
			user = new User()
			{
				Id = Guid.NewGuid(),
				Email = email,
				HashedPassword = HashingService.Hash(Guid.NewGuid().ToString()), // zeby nie bylo puste musi jakies byc bo model wymaga, do poprawienia potem
				FirstName = firstName ?? string.Empty,
				LastName = lastName ?? string.Empty,
				LastLoggedInAt = DateTime.UtcNow,
			};
			user.EmailConfirmation.IsConfirmed = true;
			await dbContext.Users.AddAsync(user, cancellationToken);
			await dbContext.SaveChangesAsync(cancellationToken);
			var tokens = tokenProvider.Generate(user);
			var refreshToken = request.HttpContext.GetRefreshToken();
			if (await tokenProvider.GetTokenIdentifiers(refreshToken) is { } tokenIdentifiers)
				user.Refresh(tokenIdentifiers.TokenId, tokens, ipAddressLocation);
			else
				user.Login(tokens, ipAddressLocation);
		}
		return Results.Ok(UserReadModel.From(user));
	}
}