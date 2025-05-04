using API.Database;
using API.Modules.Users.ValueObjects;
using System.IdentityModel.Tokens.Jwt;

namespace API.Modules.Users.Services;

internal class UserTokensHttpStorage(IHttpContextAccessor httpContextAccessor)
{
	public HttpContext HttpContext => httpContextAccessor.HttpContext!;

	public const string AccessTokenCookieKey = "cadr_access_token";
	public const string RefreshTokenCookieKey = "cadr_refresh_token";

	public void Set(UserTokens tokens)
	{
		var (accessToken, refreshToken) = tokens;

		var accessTokenCookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = accessToken.ExpiresAt,
		};

		var refreshTokenCookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = refreshToken.ExpiresAt,
		};

		Clear();
		HttpContext.Response.Cookies.Append(AccessTokenCookieKey, accessToken.Value, accessTokenCookieOptions);
		HttpContext.Response.Cookies.Append(RefreshTokenCookieKey, refreshToken.Value, refreshTokenCookieOptions);
	}

	public void Clear()
	{
		HttpContext.Response.Cookies.Delete(AccessTokenCookieKey);
		HttpContext.Response.Cookies.Delete(RefreshTokenCookieKey);
	}

	public string? GetAccessToken()
		=> HttpContext.Request.Cookies[AccessTokenCookieKey];

	public string? GetRefreshToken()
		=> HttpContext.Request.Cookies[RefreshTokenCookieKey];
}