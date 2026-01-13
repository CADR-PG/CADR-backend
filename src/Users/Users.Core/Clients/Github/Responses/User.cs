namespace Users.Core.Clients.Github.Responses;

internal sealed class User
{
	public required string Login { get; init; }
	public required string? Name { get; init; }
	public required string Email { get; init; }
}