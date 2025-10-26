using Shared.ValueObjects;
using Users.Core.ValueObjects;

namespace Users.Core.Entities;

internal class User
{
	public required UserId Id { get; init; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public string FullName => $"{FirstName} {LastName}";

	public required string Email { get; set; }
	public EmailConfirmation EmailConfirmation { get; init; } = new();
	public required string HashedPassword { get; set; }
	public required DateTime LastLoggedInAt { get; set; }
	public List<RefreshToken> RefreshTokens { get; set; } = [];

	public void SetupEmailConfirmation()
	{
		var sentAt = DateTime.UtcNow;

		EmailConfirmation.IsConfirmed = false;
		EmailConfirmation.Code = Random.Shared.Next(0, 1_000_000);
		EmailConfirmation.SentAt = sentAt;
		EmailConfirmation.ExpiresAt = sentAt.AddDays(1);
	}

	public bool ConfirmEmail(int requestCode)
	{
		if (!(EmailConfirmation.ExpiresAt > DateTime.UtcNow) || EmailConfirmation.Code != requestCode)
			return false;

		EmailConfirmation.IsConfirmed = true;
		EmailConfirmation.Code = null;
		EmailConfirmation.SentAt = null;
		EmailConfirmation.ExpiresAt = null;
		return true;

	}

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