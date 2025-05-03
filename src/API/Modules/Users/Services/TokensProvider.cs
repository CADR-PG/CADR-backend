using API.Modules.Users.Entities;
using API.Modules.Users.Settings;
using API.Modules.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace API.Modules.Users.Services;

internal interface IUserTokensProvider
{
	public UserTokens Generate(User user);
}

internal sealed class JwtProvider(IOptions<UserTokensSettings> jwtSettings) : IUserTokensProvider
{
	private UserTokensSettings Settings => jwtSettings.Value;
	private static readonly JsonWebTokenHandler JsonWebTokenHandler = new();

	public UserTokens Generate(User user) => new(GenerateAccessToken(user), GenerateRefreshToken(user));

	private UserToken GenerateAccessToken(User user)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.AccessSecret));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var expiresAt = DateTime.UtcNow.Add(Settings.AccessLifetime);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(

			[
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
			]),
			Expires = expiresAt,
			SigningCredentials = credentials,
			Issuer = Settings.Issuer,
			Audience = Settings.Audience,
		};

		var accessToken = JsonWebTokenHandler.CreateToken(tokenDescriptor);
		return new UserToken(accessToken, expiresAt, null);
	}

	private UserToken GenerateRefreshToken(User user)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.RefreshSecret));
		var randomBytes = new byte[64];
		using (var rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(randomBytes);
		}

		var code = Convert.ToBase64String(randomBytes);

		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var expiresAt = DateTime.UtcNow.Add(Settings.RefreshLifetime);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(
			[
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
				new Claim("code", code)
			]),
			Expires = expiresAt,
			SigningCredentials = credentials,
			Issuer = Settings.Issuer,
			Audience = Settings.Audience,
		};

		var hashedRefreshToken = new RefreshToken()
		{
			Id = Guid.NewGuid(),
			HashedCode = code,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = expiresAt,
			UserId = user.Id
		};

		var refreshToken = JsonWebTokenHandler.CreateToken(tokenDescriptor);
		return new UserToken(refreshToken, expiresAt, hashedRefreshToken);
	}
}