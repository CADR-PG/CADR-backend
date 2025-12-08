using Shared.ValueObjects;
using Users.Core.Clients.IpApi.Responses;
using Users.Core.ReadModels;

namespace Users.Core.Entities;

internal enum AuthenticationType
{
	Login = 0,
	Refresh = 1,
}

internal sealed class UserLocationLog
{
	public required Guid Id { get; init; }
	public required UserId UserId { get; init; }

	public required AuthenticationType AuthenticationType { get; set; }
	public required DateTime OccuredAt { get; set; }

	public required string IpAddress { get; init; }

	public required double Latitude { get; init; }
	public required double Longitude { get; init; }

	public required string Country { get; init; }
	public required string City { get; init; }

	public static UserLocationLog From(User user, IpAddressLocationReadModel ipAddressLocation, AuthenticationType type) => new()
	{
		Id = Guid.NewGuid(),
		UserId = user.Id,
		AuthenticationType = type,
		OccuredAt = user.LastLoggedInAt,
		IpAddress = ipAddressLocation.IpAddress,
		Latitude = ipAddressLocation.Latitude,
		Longitude = ipAddressLocation.Longitude,
		Country = ipAddressLocation.Country,
		City = ipAddressLocation.City
	};

	public static double CalculateDistanceKm(UserLocationLog from, UserLocationLog to)
	{
		const double earthRadius = 6_371;

		var dLatitude = ToRadians(to.Latitude - from.Latitude);
		var dLongitude = ToRadians(to.Longitude - to.Longitude);

		var fromLatitude = ToRadians(from.Latitude);
		var toLatitude = ToRadians(to.Latitude);

		var a = Math.Sin(dLatitude / 2) * Math.Sin(dLatitude / 2) +
		           Math.Cos(fromLatitude) * Math.Cos(toLatitude) *
		           Math.Sin(dLongitude / 2) * Math.Sin(dLongitude / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

		return earthRadius * c;

		static double ToRadians(double deg) => deg * Math.PI / 180;
	}

	public static bool IsDistanceIssue(UserLocationLog from, UserLocationLog to, double distanceKm)
		=> to.OccuredAt - from.OccuredAt < TimeSpan.FromHours(1) && distanceKm > 100;
}