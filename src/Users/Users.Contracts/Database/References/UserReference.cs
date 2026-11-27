using Shared.ValueObjects;

namespace Users.Contracts.Database.References;

public class UserReference
{
	public required UserId Id { get; init; }
	public required string Email { get; init; }
}