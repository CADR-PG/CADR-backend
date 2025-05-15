using Shared.ValueObjects;

namespace Users.Core.ValueObjects;

internal sealed record UserTokens(UserToken AccessToken, UserToken RefreshToken, DateTime CreatedAt);

internal sealed record UserToken(TokenId Id, string Value, DateTime ExpiresAt);

internal sealed record UserTokenIdentifiers(UserId UserId, TokenId TokenId);