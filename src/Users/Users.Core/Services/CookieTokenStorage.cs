using Microsoft.AspNetCore.Http;
using Users.Core.ValueObjects;

namespace Users.Core.Services;

internal static class CookieTokenStorage
{
	public const string AccessTokenCookieKey = "cadr_access_token";
	public const string RefreshTokenCookieKey = "cadr_refresh_token";

	public static void SetTokenCookies(this HttpContext httpContext, UserTokens tokens)
	{
		var (accessToken, refreshToken, _) = tokens;

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

		httpContext.Response.Cookies.Append(AccessTokenCookieKey, accessToken.Value, accessTokenCookieOptions);
		httpContext.Response.Cookies.Append(RefreshTokenCookieKey, refreshToken.Value, refreshTokenCookieOptions);
	}

	public static void ClearTokenCookies(this HttpContext httpContext)
	{
		httpContext.Response.Cookies.Delete(AccessTokenCookieKey);
		httpContext.Response.Cookies.Delete(RefreshTokenCookieKey);
	}

	public static string? GetAccessToken(this HttpContext httpContext)
		=> httpContext.Request.Cookies[AccessTokenCookieKey];

	public static string? GetRefreshToken(this HttpContext httpContext)
		=> httpContext.Request.Cookies[RefreshTokenCookieKey];
}