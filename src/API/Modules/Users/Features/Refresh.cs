
using API.Database;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;


namespace API.Modules.Users.Features;


internal record RefreshToken([FromBody] RefreshToken.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Token, string RefreshToken);
};

internal record RefreshReadModel(string token, string refreshToken);

internal sealed class RefreshEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<RefreshToken, RefreshHandler>("refresh");
}

internal sealed class RefreshHandler(
	CADRDbContext dbContext,
	TokenProvider tokenProvider,
	IHttpContextAccessor httpContextAccessor
) : IHttpRequestHandler<RefreshToken>
{
	public async Task<IResult> Handle(RefreshToken request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;
		var handler = new JwtSecurityTokenHandler();
		var jwtSecurityToken = handler.ReadJwtToken(credentials.Token);
		var userId = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Id.ToString() == userId, cancellationToken);
		if (user is null)
			return Results.NotFound();

		var newToken = tokenProvider.Create(user);
		var newRefreshToken = tokenProvider.GenerateRefreshToken(user);

		var cookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = DateTime.UtcNow.AddHours(1)
		};

		httpContextAccessor.HttpContext!.Response.Cookies.Delete("jwt");
		httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken");

		httpContextAccessor.HttpContext.Response.Cookies.Append("jwt", newToken, cookieOptions);
		httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

		return Results.Ok(new RefreshReadModel(newToken, newRefreshToken));
	}
}
