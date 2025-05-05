using API.Modules.Users.ValueObjects;

namespace API.Modules.Users.Entities;

internal class RefreshToken
{
	public required Guid Id { get; init; }
	public required string HashedCode { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime ExpiresAt { get; init; }
	public required UserId UserId { get; init; }
}