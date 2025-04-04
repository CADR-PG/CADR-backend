using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Modules.Users.Features;

internal record Login([FromBody] Login.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email, string Password);
}

internal record UserReadModel(string response);


internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login");
}

internal sealed class LoginHandler(
	CADRDbContext dbContext
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;
		var passwordHasher = new PasswordHasher<User>();
		
		// Check if the user exists
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Email == credentials.Email, cancellationToken);
		if (user is null)
			return Results.NotFound();

		// Verify the password
		var result = passwordHasher.VerifyHashedPassword(user, user.Password, credentials.Password);
		if (result == PasswordVerificationResult.Success)
		{
			return Results.Ok(new UserReadModel("Login successfully"));
		}
		return Results.Unauthorized();
	}
}
