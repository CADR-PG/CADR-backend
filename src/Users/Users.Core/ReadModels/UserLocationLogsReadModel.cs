using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal sealed record UserLocationLogsReadModel
{
	public required IEnumerable<Entry> Logs { get; init; }

	public sealed record Entry
	{
		public required AuthenticationType AuthenticationType { get; set; }
		public required DateTime OccuredAt { get; set; }

		public required string IpAddress { get; init; }

		public required double Latitude { get; init; }
		public required double Longitude { get; init; }

		public required string Country { get; init; }
		public required string City { get; init; }

		public required double DistanceFromLastLocationKm { get; set; }
		public required bool IsDistanceIssue { get; set; }
	}
}