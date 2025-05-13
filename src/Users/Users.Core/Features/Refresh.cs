using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Exceptions;
using Shared.ValueObjects;
using System.IdentityModel.Tokens.Jwt;
using Users.Core.Database;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct Refresh(HttpContext HttpContext) : IHttpRequest;

internal sealed class RefreshEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Refresh, RefreshHandler>("/refresh");
}

internal sealed class RefreshHandler(
	UsersDbContext dbContext,
	ITokenProvider tokenProvider
) : IHttpRequestHandler<Refresh>
{
	public async Task<IResult> Handle(Refresh request, CancellationToken cancellationToken)
	{
		var refreshToken = request.HttpContext.GetRefreshToken();

		if (refreshToken is null || await tokenProvider.GetTokenIdentifiers(refreshToken) is not { } identifiers)
		{
			throw new CadrException("Invalid refresh token");
		}

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.FirstAsync(x => x.Id == identifiers.UserId, cancellationToken);

		var refreshedUserTokens = tokenProvider.Generate(user);

		user.Refresh(identifiers.TokenId, refreshedUserTokens);
		await dbContext.SaveChangesAsync(cancellationToken);

		request.HttpContext.ClearTokenCookies();

		return Results.NoContent();
	}
}