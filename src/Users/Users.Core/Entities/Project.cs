using System.Text.Json;

namespace Users.Core.Entities;

internal class Project
{
	public required Guid Id { get; init; }
	public required string? Name { get; set; }
	public required string? Description { get; set; }
	public required Dictionary<string, object> JsonDocument { get; set; } = new();

	public Guid UserId { get; init; }
	public User? User { get; init; }
}