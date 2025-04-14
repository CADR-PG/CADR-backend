using API.Modules.Users.Options;
using Microsoft.Extensions.Options;

namespace API.Modules.Users.Infrastructure;

internal class OptionsInjector
{
	private readonly JwtOptions jwtOptions;
	private readonly RefreshTokenOptions refreshTokenOptions;

	public OptionsInjector(IOptions<JwtOptions> jwtOptions, IOptions<RefreshTokenOptions> refreshTokenOptions)
	{
		this.jwtOptions = jwtOptions.Value;
		this.refreshTokenOptions = refreshTokenOptions.Value;
	}


	// Jwt authentication
	public string GetSecret()
	{
		var secretKey = jwtOptions.Secret;
		if (string.IsNullOrEmpty(secretKey))
			throw new InvalidOperationException("Secret key is not configured.");

		return secretKey;
	}

	public string GetIssuer()
	{
		var issuer = jwtOptions.Issuer;
		if (string.IsNullOrEmpty(issuer))
			throw new InvalidOperationException("Issuer is not configured.");

		return issuer;
	}

	public string GetAudience()
	{
		var audience = jwtOptions.Audience;
		if (string.IsNullOrEmpty(audience))
			throw new InvalidOperationException("Audience is not configured.");

		return audience;
	}

	public int GetExpireTimeInDays()
	{
		var expireTimeInDays = jwtOptions.ExpireTimeInDays;
		if (expireTimeInDays <= 0)
			throw new InvalidOperationException("Expire time in days is not configured.");

		return expireTimeInDays;
	}

	// Refresh token authentication

	public string GetRefreshSecret()
	{
		var secretKey = refreshTokenOptions.Secret;
		if (string.IsNullOrEmpty(secretKey))
			throw new InvalidOperationException("Refresh token secret key is not configured.");

		return secretKey;
	}

	public int GetRefreshExpireTimeInDays()
	{
		var expireTimeInDays = refreshTokenOptions.ExpireTimeInDays;
		if (expireTimeInDays <= 0)
			throw new InvalidOperationException("Refresh token expire time in days is not configured.");

		return expireTimeInDays;
	}
}