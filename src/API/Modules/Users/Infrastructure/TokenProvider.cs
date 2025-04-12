using API.Modules.Users.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace API.Modules.Users.Infrastructure;

internal sealed class TokenProvider(IConfiguration configuration)
{
	public string Create(User user)
{
	string secretKey = configuration["Jwt:Secret"]!;
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
			Expires = DateTime.UtcNow.AddDays(configuration.GetValue<int>("Jwt:ExpirationDays")),
			SigningCredentials = credentials,
			Issuer = configuration["Jwt:Issuer"],
			Audience = configuration["Jwt:Audience"],
	};

var handler = new JsonWebTokenHandler();

string token = handler.CreateToken(tokenDescritpor);
		return token;
	}

	public string GenerateRefreshToken(User user)
	{
		string secretKey = configuration["Jwt:Secret"]!;
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
			Expires = DateTime.UtcNow.AddDays(configuration.GetValue<int>("RefreshToken:ExpireTimeInDays")),
			SigningCredentials = credentials,
			Issuer = configuration["RefreshToken:Issuer"],
			Audience = configuration["RefreshToken:Audience"],
		};
		var handler = new JsonWebTokenHandler();
		string refreshToken = handler.CreateToken(tokenDescritpor);
		return refreshToken;
	}
}