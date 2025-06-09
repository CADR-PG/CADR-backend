using Shared.ValueObjects;

namespace Users.Core.Entities;

internal class Project
{
	public required Guid Id { get; init; }
	public required string? Name { get; set; }
	public required string? Description { get; set; }

	public required DateTime LastUpdate { get; set; }
	public string? JsonDocument { get; set; }

	public UserId UserId { get; init; }
	public User? User { get; init; }
}