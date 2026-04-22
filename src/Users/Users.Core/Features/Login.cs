using FluentValidation;
using Microsoft.AspNetCore.Http;
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

internal record Login([FromBody] Login.Credentials Body, HttpContext HttpContext) : IHttpRequest
{
	internal record Credentials(string Email, string Password);
}

internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login")
			.Produces<UserReadModel>()
			.ProducesError(400,
				$"`{nameof(Shared.Endpoints.SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.")
			.AddValidation<Login.Credentials>();
}

internal sealed class LoginHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider,
	IIpApiClient ipApiClient,
	UserMailingService userMailingService
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var (email, password) = request.Body;

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.Include(x => x.UserLocationLogs)
			.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
		if (user is null || !HashingService.IsValid(password, user.HashedPassword))
			return Errors.InvalidLoginCredentialsError;

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

		try
		{
			var lastUserLoginLocation = user.UserLocationLogs.MaxBy(x => x.OccuredAt);

			var userSafeAccessPoints = await dbContext.UserSafeAccessPoints
				.Where(x => x.UserId == user.Id)
				.ToListAsync(cancellationToken);

			if (lastUserLoginLocation is null || userSafeAccessPoints.All(x => UserLocationLog.CalculateDistanceKm(x, lastUserLoginLocation) > x.Radius))
				await userMailingService.SendUserLoggedIn(user);
		}
#pragma warning disable CA1031
		catch
#pragma warning restore CA1031
		{
			// ignored
		}

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class LoginValidator : AbstractValidator<Login.Credentials>
{
	public LoginValidator()
	{
		RuleFor(x => x.Email).NotEmpty().EmailAddress();
		RuleFor(x => x.Password).NotEmpty();
	}
}