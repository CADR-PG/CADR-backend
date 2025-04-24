using API.Database;
using API.Modules.Users.Services;
using API.Modules.Users.ValueObjects;
using API.Shared.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace API.Modules.Users.Features;

internal record Refresh : IHttpRequest;

internal sealed class RefreshEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Refresh, RefreshHandler>("/refresh");
}

internal sealed class RefreshHandler(
	CADRDbContext dbContext,
	IUserTokensProvider userTokensProvider,
	UserTokensHttpStorage userTokensHttpStorage
) : IHttpRequestHandler<Refresh>
{

	private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();

	public async Task<IResult> Handle(Refresh request, CancellationToken cancellationToken)
	{
		#pragma warning disable S1135
		// TODO MOVE TO DB
		var refreshToken = userTokensHttpStorage.GetRefreshToken();
		if (refreshToken is null) return Results.Unauthorized();
		var jwtSecurityToken = JwtSecurityTokenHandler.ReadJwtToken(refreshToken);
		var userId = new UserId(Guid.Parse(jwtSecurityToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value));
		var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null) return Results.Problem(statusCode: 400, title: "InvalidRefreshToken", detail: $"Refresh token `{refreshToken}` contains invalid credentials.");

		var tokens = userTokensProvider.Generate(user);
		userTokensHttpStorage.Set(tokens);

		return Results.NoContent();
	}
}