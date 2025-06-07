using Shared.ValueObjects;
using Users.Core.ValueObjects;

namespace Users.Core.Entities;

internal class User
{
	public required UserId Id { get; init; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string Email { get; set; }
	public required string HashedPassword { get; set; }
	public required DateTime LastLoggedInAt { get; set; }
	public List<RefreshToken> RefreshTokens { get; set; } = [];

	public List<Project> Projects { get; set; } = [];

	public void Login(UserTokens userTokens)
	{
		var (tokenId, _, expiresAt) = userTokens.RefreshToken;

		RefreshTokens.Add(new RefreshToken
		{
			Id = tokenId,
			ExpiresAt = expiresAt,
			CreatedAt = userTokens.CreatedAt,
			UserId = Id.Value
		});

		LastLoggedInAt = userTokens.CreatedAt;
		CleanRefreshTokens();
	}

	public void Refresh(TokenId refreshTokenId, UserTokens refreshedUserTokens)
	{
		var (tokenId, _, expiresAt) = refreshedUserTokens.RefreshToken;

		RefreshTokens.Add(new RefreshToken
		{
			Id = tokenId,
			ExpiresAt = expiresAt,
			CreatedAt = refreshedUserTokens.CreatedAt,
			UserId = Id
		});

		CleanRefreshTokens(refreshTokenId);
	}

	public void Logout(TokenId? refreshTokenId)
	{
		CleanRefreshTokens(refreshTokenId);
	}


	private void CleanRefreshTokens(TokenId? refreshTokenId = null)
	{
		if (refreshTokenId is null)
			RefreshTokens.RemoveAll(x => x.ExpiresAt < DateTime.UtcNow);
		else
			RefreshTokens.RemoveAll(x => x.ExpiresAt < DateTime.UtcNow || x.Id == refreshTokenId);
	}
}