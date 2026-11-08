using Projects.Core.ValueObjects;
using Shared.ValueObjects;

namespace Projects.Core.Entities.Teams;

public sealed class Team
{
	public TeamId Id { get; private init; }
	public required string Name { get; set; }
	public string? Description { get; private set; }
	public UserId CreatedBy { get; private init; }
	public DateTimeOffset CreatedAt { get; private init; }
	public DateTimeOffset? UpdatedAt { get; set; }

	public ICollection<TeamMember> Members { get; private set; } = [];

	private Team()
	{
	}

	public static Team Create(string name, UserId createdBy, TeamId id, DateTimeOffset createdAt) => new()
	{
		Id = id,
		Name = name,
		CreatedBy = createdBy,
		CreatedAt = createdAt
	};
}
