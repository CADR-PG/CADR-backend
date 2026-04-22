using Shared.ValueObjects;
using Users.Core.Clients.IpApi.Responses;
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
	public Guid? PasswordResetToken { get; set; }
	public DateTime? PasswordResetExpiresAt { get; set; }
	public required DateTime LastLoggedInAt { get; set; }
	public List<RefreshToken> RefreshTokens { get; set; } = [];
	public List<UserLocationLog> UserLocationLogs { get; init; } = [];
	public List<UserSafeAccessPoint> UserSafeAccessPoints { get; init; } = [];

	public void SetupEmailConfirmation(string email)
	{
		var sentAt = DateTime.UtcNow;

		EmailConfirmation.IsConfirmed = false;
		EmailConfirmation.Code = $"{Random.Shared.Next(1_000_000):D6}";
		EmailConfirmation.SentAt = sentAt;
		EmailConfirmation.ExpiresAt = sentAt.AddDays(1);
		EmailConfirmation.Email = email;
	}

	public bool ConfirmEmail(string requestCode)
	{
		if (EmailConfirmation.ExpiresAt < DateTime.UtcNow || EmailConfirmation.Code != requestCode)
			return false;

		EmailConfirmation.IsConfirmed = true;
		EmailConfirmation.Code = null;
		EmailConfirmation.SentAt = null;
		EmailConfirmation.ExpiresAt = null;
		EmailConfirmation.Email = null;
		return true;
	}

	public void Login(UserTokens userTokens, IpAddressLocationReadModel ipAddressLocation)
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

		var locationLog = UserLocationLog.From(this, ipAddressLocation, AuthenticationType.Login);
		UserLocationLogs.Add(locationLog);
	}

	public void Refresh(TokenId refreshTokenId, UserTokens refreshedUserTokens, IpAddressLocationReadModel ipAddressLocation)
	{
		var (tokenId, _, expiresAt) = refreshedUserTokens.RefreshToken;

		RefreshTokens.Add(new RefreshToken
		{
			Id = tokenId,
			ExpiresAt = expiresAt,
			CreatedAt = refreshedUserTokens.CreatedAt,
			UserId = Id
		});

		LastLoggedInAt = refreshedUserTokens.CreatedAt;
		CleanRefreshTokens(refreshTokenId);

		var locationLog = UserLocationLog.From(this, ipAddressLocation, AuthenticationType.Refresh);
		UserLocationLogs.Add(locationLog);
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