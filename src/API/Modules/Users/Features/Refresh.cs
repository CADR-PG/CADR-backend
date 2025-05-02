using API.Database;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Modules.Users.Services;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;


namespace API.Modules.Users.Features;


internal record RefreshToken() : IHttpRequest
{

};

internal record RefreshReadModel(string response);

internal sealed class RefreshEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<RefreshToken, RefreshHandler>("refresh");
}

internal sealed class RefreshHandler(
	CADRDbContext dbContext,
	UserTokenAuthenticator userTokenAuthenticator
) : IHttpRequestHandler<RefreshToken>
{
	public async Task<IResult> Handle(RefreshToken request, CancellationToken cancellationToken)
	{
		var handler = new JwtSecurityTokenHandler();
		var jwtSecurityToken = handler.ReadJwtToken(userTokenAuthenticator.GetToken());
		var userId = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Id.ToString() == userId, cancellationToken);
		if (user is null)
			return Results.NotFound();

		userTokenAuthenticator.ClearTokens();
		userTokenAuthenticator.SetTokens(user);

		return Results.Ok(new RefreshReadModel("Refreshed tokens"));
	}
}