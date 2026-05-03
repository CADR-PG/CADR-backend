using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Users.Core.Clients.Github;
using Users.Core.Clients.IpApi;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record GithubLogin([FromBody] GithubLogin.Credentials Body, HttpContext HttpContext) : IHttpRequest
{
	internal record Credentials(string Code);
}

internal sealed class GithubLoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<GithubLogin, GithubLoginHandler>("github-login")
			.ProducesError(400,
				$"`{nameof(SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.")
			.AddValidation<GithubLogin.Credentials>();
}

internal sealed class GithubLoginHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider,
	IIpApiClient ipApiClient,
	IGithubOAuthClient githubOAuthClient,
	IGithubClient githubClient,
	CookieTokenStorage cookieTokenStorage
	) : IHttpRequestHandler<GithubLogin>
{
	public async Task<IResult> Handle(GithubLogin request, CancellationToken cancellationToken)
	{
		var token = await githubOAuthClient.ExchangeCodeForToken(request.Body.Code);
		var githubUser = await githubClient.GetUser(token.AccessToken);

		var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == githubUser.Email, cancellationToken);
		if (user is null)
		{
			user = new User
			{
				Id = Guid.NewGuid(),
				FirstName = githubUser.Name ?? githubUser.Login,
				LastName = string.Empty,
				Email = githubUser.Email,
				HashedPassword = HashingService.Hash(Guid.NewGuid().ToString()),
				LastLoggedInAt = DateTime.UtcNow,
			};
			await dbContext.Users.AddAsync(user, cancellationToken);
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		var ipAddress = request.HttpContext.GetClientIpAddress();
		var ipAddressLocation = await ipApiClient.GetIpAddressGeolocationData(ipAddress);

		var tokens = tokenProvider.Generate(user);
		var refreshToken = CookieTokenStorage.GetRefreshToken(request.HttpContext);
		if (await tokenProvider.GetTokenIdentifiers(refreshToken) is { } tokenIdentifiers)
			user.Refresh(tokenIdentifiers.TokenId, tokens, ipAddressLocation);
		else
			user.Login(tokens, ipAddressLocation);

		await dbContext.SaveChangesAsync(cancellationToken);
		cookieTokenStorage.SetTokenCookies(request.HttpContext, tokens);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class GithubLoginValidator : AbstractValidator<GithubLogin.Credentials>
{
	public GithubLoginValidator()
	{
		RuleFor(x => x.Code).NotEmpty();
	}
}