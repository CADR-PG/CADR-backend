using Shared.ValueObjects;
using Users.Core.ValueObjects;

namespace Users.Core.Entities;

internal class RefreshToken
{
	public required TokenId Id { get; init; }
	public required DateTime ExpiresAt { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required UserId UserId { get; init; }
}