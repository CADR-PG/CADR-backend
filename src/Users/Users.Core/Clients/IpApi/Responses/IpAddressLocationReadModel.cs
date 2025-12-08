using System.Text.Json.Serialization;

namespace Users.Core.Clients.IpApi.Responses;

internal sealed record IpAddressLocationReadModel
{
	[JsonPropertyName("ip")] public required string IpAddress { get; init; }
	public required double Latitude { get; init; }
	public required double Longitude { get; init; }

	[JsonPropertyName("country_name")] public required string Country { get; init; }
	public required string City { get; init; }
}