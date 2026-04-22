using Shared.ValueObjects;
using Users.Core.Entities.Contracts;

namespace Users.Core.Entities;

internal sealed class UserSafeAccessPoint : IGeoPoint
{
	public required Guid Id { get; init; }
	public required UserId UserId { get; init; }

	public required string Name { get; set; }

	public required DateTime CreatedAt { get; init; }
	public required DateTime? EditedAt { get; set; }

	public required double Latitude { get; set; }
	public required double Longitude { get; set; }

	public required double Radius { get; set; }
}