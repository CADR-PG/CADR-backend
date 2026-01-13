using System.Text.Json.Serialization;

namespace Users.Core.Clients.Github.Requests;

internal sealed class GitHubAccessTokenRequest
{
	[JsonPropertyName("client_id")]
	public required string ClientId { get; init; }

	[JsonPropertyName("client_secret")]
	public required string ClientSecret { get; init; }

	[JsonPropertyName("code")]
	public required string Code { get; init; }

	[JsonPropertyName("redirect_uri")]
	public string? RedirectUri { get; init; }
}