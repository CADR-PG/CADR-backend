using API.Modules.Users.ValueObjects;

namespace API.Modules.Users.Entities;

internal class User
{
	public required UserId Id { get; init; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string Email { get; set; }
	public required string HashedPassword { get; set; }
}