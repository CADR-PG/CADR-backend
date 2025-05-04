using API.Database;
using API.Modules.Users.Services;
using API.Modules.Users.ValueObjects;
using API.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace API.Modules.Users.Features;

internal record Logout : IHttpRequest;

internal sealed class LogoutEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Logout, LogoutHandler>("/logout");
}

internal sealed class LogoutHandler(
	CADRDbContext dbContext,
	UserTokensHttpStorage userTokensHttpStorage
) : IHttpRequestHandler<Logout>
{
	public async Task<IResult> Handle(Logout request, CancellationToken cancellationToken)
	{
		var refreshToken = userTokensHttpStorage.GetRefreshToken();
		if (refreshToken is null)
		{
			return Results.BadRequest("Brak tokenu odświeżania");
		}

		var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshToken);
		var userId = jwtSecurityToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
		var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == new UserId(Guid.Parse(userId)), cancellationToken);

		if (token is not null)
		{
			dbContext.RefreshTokens.Remove(token);
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		userTokensHttpStorage.Clear();
		return Results.NoContent();
	}
}