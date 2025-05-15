using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Users.Core.Database;
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
			.ProducesError(400, $"`{nameof(Shared.Endpoints.SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidLoginCredentialsError)}`")
			.WithDescription($"Login with email and password. Returns `{nameof(UserReadModel)}` on success.")
			.AddValidation<Login.Credentials>();
}

internal sealed class LoginHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var (email, password) = request.Body;

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
		if (user is null || !HashingService.IsValid(password, user.HashedPassword))
			return Errors.InvalidLoginCredentialsError;

		var tokens = tokenProvider.Generate(user);
		user.Login(tokens);
		await dbContext.SaveChangesAsync(cancellationToken);

		request.HttpContext.SetTokenCookies(tokens);

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