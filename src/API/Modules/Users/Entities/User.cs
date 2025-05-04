using API.Modules.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Modules.Users.Entities;

internal class User
{
	public required UserId Id { get; init; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string Email { get; set; }
	public required string HashedPassword { get; set; }
	public required List<RefreshToken> RefreshTokens { get; set; } = new();

	private void CleanRefreshTokens()
	{
		RefreshTokens.RemoveAll(x => x.ExpiresAt < DateTime.UtcNow);
	}

	public void AddRefreshToken(RefreshToken refreshToken)
	{
		CleanRefreshTokens();
		RefreshTokens.Add(refreshToken);
	}
}