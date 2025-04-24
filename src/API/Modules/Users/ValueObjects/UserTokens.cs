namespace API.Modules.Users.ValueObjects;

internal sealed record UserTokens(UserToken AccessToken, UserToken RefreshToken);

internal sealed record UserToken(string Value, DateTime ExpiresAt);