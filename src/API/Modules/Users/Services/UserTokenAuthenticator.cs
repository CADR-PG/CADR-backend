using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Modules.Users.Options;
using Microsoft.Extensions.Options;

namespace API.Modules.Users.Services;

internal class UserTokenAuthenticator
{
	internal required TokenProvider tokenProvider;
	internal required IHttpContextAccessor httpContextAccessor;

	public UserTokenAuthenticator(TokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor, IOptions<JwtOptions> jwtOptions, IOptions<RefreshTokenOptions> refreshTokenOptions)
	{
		this.tokenProvider = tokenProvider;
		this.httpContextAccessor = httpContextAccessor;
	}

	public string? GetToken()
	{
		var token = httpContextAccessor.HttpContext?.Request.Cookies["jwt"];
		if (string.IsNullOrEmpty(token))
			return null;

		return token;
	}

	public string? GetRefreshToken()
	{
		var refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
		if (string.IsNullOrEmpty(refreshToken))
			return null;

		return refreshToken;
	}

	public void SetTokens(User user)
	{
		var token = tokenProvider.Create(user);
		var refreshToken = tokenProvider.GenerateRefreshToken(user);

		var cookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = DateTime.UtcNow.AddHours(1)
		};

		httpContextAccessor.HttpContext!.Response.Cookies.Append("jwt", token, cookieOptions);
		httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
	}

	public void ClearTokens()
	{
		httpContextAccessor.HttpContext!.Response.Cookies.Delete("jwt");
		httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken");
	}
}