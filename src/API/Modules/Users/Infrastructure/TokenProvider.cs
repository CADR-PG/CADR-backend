using API.Modules.Users.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Security.Claims;
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
			Expires = DateTime.UtcNow.AddDays(7),
			SigningCredentials = credentials,
			Issuer = configuration["Jwt:Issuer"],
			Audience = configuration["Jwt:Audience"],
		};

		var handler = new JsonWebTokenHandler();

		string token = handler.CreateToken(tokenDescritpor);
		return token;
	}
}
