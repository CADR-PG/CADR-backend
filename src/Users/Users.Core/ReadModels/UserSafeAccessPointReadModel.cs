using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal sealed record UserSafeAccessPointReadModel
{
	public required string Id { get; init; }

	public required string Name { get; init; }
	public required DateTime CreatedAt { get; init; }
	public required DateTime? EditedAt { get; init; }

	public required double Latitude { get; init; }
	public required double Longitude { get; init; }
	public required double Radius { get; init; }

	public static UserSafeAccessPointReadModel From(UserSafeAccessPoint point) => new()
	{
		Id = point.Id.ToString(),
		Latitude = point.Latitude,
		Longitude = point.Longitude,
		Name = point.Name,
		CreatedAt = point.CreatedAt,
		EditedAt = point.EditedAt,
		Radius = point.Radius
	};
}