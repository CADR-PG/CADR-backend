using API.Modules.Users.ValueObjects;

namespace API.Modules.Users.Entities;

internal class RefreshToken
{
	public required Guid Id { get; init; }
	public required string HashedCode { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required DateTime ExpiresAt { get; set; }
	public required UserId UserId { get; set; }
}