using API.Modules.Users.Models;
using API.Modules.Users.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace API.Modules.Users.Infrastructure;

internal sealed class TokenProvider(OptionsInjector optionsInjector)
{
	public string Create(User user)
	{
		string secretKey = optionsInjector.GetSecret();
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var tokenDescritpor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(

			[
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
				new Claim("email_verified", user.Email)
			]),
			Expires = DateTime.UtcNow.AddDays(optionsInjector.GetExpireTimeInDays()),
			SigningCredentials = credentials,
			Issuer = optionsInjector.GetIssuer(),
			Audience = optionsInjector.GetAudience(),
		};

		var handler = new JsonWebTokenHandler();

		string token = handler.CreateToken(tokenDescritpor);
		return token;
	}

	public string GenerateRefreshToken(User user)
	{
		string secretKey = optionsInjector.GetRefreshSecret();
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var tokenDescritpor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(
			[
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			]),
			Expires = DateTime.UtcNow.AddDays(optionsInjector.GetRefreshExpireTimeInDays()),
			SigningCredentials = credentials,
		};
		var handler = new JsonWebTokenHandler();
		string refreshToken = handler.CreateToken(tokenDescritpor);
		return refreshToken;
	}
}