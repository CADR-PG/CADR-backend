using API.Database;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
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

internal record UserReadModel(string token);


internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login");
}

internal sealed class LoginHandler(
	CADRDbContext dbContext, TokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;
		var passwordHasher = new PasswordHasher<User>();

		// Check if the user exists
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Email.Trim() == credentials.Email.Trim(), cancellationToken);
		if (user is null)
			return Results.NotFound();

		// Verify the password
		var result = passwordHasher.VerifyHashedPassword(user, user.Password.Trim(), credentials.Password.Trim());
		if (result == PasswordVerificationResult.Success)
		{
			var token = tokenProvider.Create(user);

			var cookieOptions = new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Strict,
				Expires = DateTime.UtcNow.AddHours(1)
			};

			// httpContextAccessor accesses the current HTTP context
			// Set the cookie with the token
			httpContextAccessor.HttpContext!.Response.Cookies.Append("jwt", token, cookieOptions);

			var response = Results.Ok(new UserReadModel(token));
			return response;
		}

		return Results.Unauthorized();
	}
}