using Shared.ValueObjects;

namespace Users.Contracts.Database.References;

public class UserReference
{
	public required UserId Id { get; init; }
	public required string FirstName { get; init; }
	public required string LastName { get; init; }
	public string FullName => $"{FirstName} {LastName}";

	public required string Email { get; init; }
}