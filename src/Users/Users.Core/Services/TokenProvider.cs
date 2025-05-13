using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Users.Core.Entities;
using Users.Core.Settings;
using Users.Core.ValueObjects;

namespace Users.Core.Services;

internal interface ITokenProvider
{
	public UserTokens Generate(User user);
	public Task<UserTokenIdentifiers?> GetTokenIdentifiers(string token);
}

internal sealed class JwtTokenProvider : ITokenProvider
{
	private readonly JwtSettings _jwtSettings;
	private readonly TokenValidationParameters _refreshTokenValidationParameters;
	private static readonly JsonWebTokenHandler JsonWebTokenHandler = new();

	public JwtTokenProvider(IOptions<JwtSettings> jwtSettings)
	{
		_jwtSettings = jwtSettings.Value;
		_refreshTokenValidationParameters = GetRefreshTokenValidationParameters(_jwtSettings);
	}

	public UserTokens Generate(User user)
	{
		var issuedAt = DateTime.UtcNow;
		return new UserTokens(GenerateAccessToken(user, issuedAt), GenerateRefreshToken(user, issuedAt), issuedAt);
	}

	private UserToken GenerateAccessToken(User user, DateTime createdAt)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.AccessSecret));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var tokenId = Guid.NewGuid();
		var expiresAt = createdAt.Add(_jwtSettings.AccessLifetime);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity([
				new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
			]),
			Expires = expiresAt,
			SigningCredentials = credentials,
			Issuer = _jwtSettings.Issuer,
			Audience = _jwtSettings.Audience,
		};

		var accessToken = JsonWebTokenHandler.CreateToken(tokenDescriptor);
		return new UserToken(tokenId, accessToken, expiresAt);
	}

	private UserToken GenerateRefreshToken(User user, DateTime createdAt)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.RefreshSecret));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var tokenId = Guid.NewGuid();
		var expiresAt = createdAt.Add(_jwtSettings.RefreshLifetime);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity([
				new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			]),
			Expires = expiresAt,
			SigningCredentials = credentials,
			Issuer = _jwtSettings.Issuer,
			Audience = _jwtSettings.Audience,
		};

		var refreshToken = JsonWebTokenHandler.CreateToken(tokenDescriptor);
		return new UserToken(tokenId, refreshToken, expiresAt);
	}

	public async Task<UserTokenIdentifiers?> GetTokenIdentifiers(string token)
	{
		if (!JsonWebTokenHandler.CanReadToken(token)) return null;

		var tokenValidationResult = await JsonWebTokenHandler.ValidateTokenAsync(token, _refreshTokenValidationParameters);

		if (!tokenValidationResult.IsValid) return null;

		var tokenId = Guid.Parse(tokenValidationResult.Claims[JwtRegisteredClaimNames.Jti].ToString()!);
		var userId = Guid.Parse(tokenValidationResult.Claims[JwtRegisteredClaimNames.Sub].ToString()!);

		return new UserTokenIdentifiers(tokenId, userId);
	}

	public static TokenValidationParameters GetAccessTokenValidationParameters(JwtSettings jwtSettings)
	{
		var parameters = GetTokenValidationParameters(jwtSettings);
		parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.AccessSecret));
		return parameters;
	}

	private static TokenValidationParameters GetRefreshTokenValidationParameters(JwtSettings jwtSettings)
	{
		var parameters = GetTokenValidationParameters(jwtSettings);
		parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.RefreshSecret));
		return parameters;
	}

	private static TokenValidationParameters GetTokenValidationParameters(JwtSettings jwtSettings) => new()
	{
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidateIssuer = true,
		ValidIssuer = jwtSettings.Issuer,
		ValidateAudience = true,
		ValidAudience = jwtSettings.Audience,
	};
}