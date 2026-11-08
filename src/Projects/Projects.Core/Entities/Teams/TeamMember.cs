using Projects.Core.ValueObjects;
using Shared.ValueObjects;

namespace Projects.Core.Entities.Teams;

public enum TeamRole
{
	Owner = 0,
	Member = 10,
	Admin = 20
}

public sealed class TeamMember
{
	public UserId UserId { get; private init; }
	public TeamId TeamId { get; private init; }
	public Team? Team { get; private init; }

	public TeamRole Role { get; private set; }
	public DateTimeOffset JoinedAt { get; private init; }
	public DateTimeOffset? LeftAt { get; set; }

	public bool IsActive => LeftAt is null;

	private TeamMember()
	{
	}

	public static TeamMember Create(TeamId teamId, UserId userId, TeamRole role, DateTimeOffset joinedAt) => new()
	{
		TeamId = teamId,
		UserId = userId,
		Role = role,
		JoinedAt = joinedAt
	};
}
