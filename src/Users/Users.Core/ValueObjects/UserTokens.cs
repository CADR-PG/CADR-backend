using Shared.ValueObjects;

namespace Users.Core.ValueObjects;

internal sealed record UserTokens(UserToken AccessToken, UserToken RefreshToken, DateTime CreatedAt);

public record UserToken(TokenId Id, string Value, DateTime ExpiresAt);

public record UserTokenIdentifiers(UserId UserId, TokenId TokenId);