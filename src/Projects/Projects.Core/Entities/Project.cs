using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Projects.Core.Entities;

public class Project
{
	public required Guid Id { get; init; }
	public required string? Name { get; set; }
	public required string? Description { get; set; }

	public required DateTime LastUpdate { get; set; }
	public string? JsonDocument { get; set; }

	public UserId UserId { get; init; }
	public UserReference? User { get; init; }
}