using API.Database;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Modules.Users.Services;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace API.Modules.Users.Features;

internal record Login([FromBody] Login.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email, string Password);
}

internal record UserReadModel(string Response);


internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login");
}

internal sealed class LoginHandler(
	CADRDbContext dbContext, UserTokenAuthenticator userTokenAuthenticator
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;
		var passwordHasher = new PasswordHasher<User>();

		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Email.Trim() == credentials.Email.Trim(), cancellationToken);
		if (user is null)
			return Results.NotFound();

		var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash.Trim(), credentials.Password.Trim());
		if (result == PasswordVerificationResult.Success)
		{
			userTokenAuthenticator.SetTokens(user);
			return Results.Ok(new UserReadModel("Logged in successfully"));
		}

		return Results.Unauthorized();
	}
}