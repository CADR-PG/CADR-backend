using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Users.Core.Database;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct Logout(HttpContext HttpContext) : IHttpRequest;

internal sealed class LogoutEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Logout, LogoutHandler>("logout");
}

internal sealed class LogoutHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider
) : IHttpRequestHandler<Logout>
{
	public async Task<IResult> Handle(Logout logout, CancellationToken cancellationToken)
	{
		var refreshToken = logout.HttpContext.GetRefreshToken();

		if (refreshToken is not null && await tokenProvider.GetTokenIdentifiers(refreshToken) is { } identifiers)
		{
			var user = await dbContext.Users
				.Include(x => x.RefreshTokens)
				.FirstAsync(x => x.Id == identifiers.UserId, cancellationToken);

			user.Logout(identifiers.TokenId);
		}

		logout.HttpContext.ClearTokenCookies();

		return Results.NoContent();
	}
}