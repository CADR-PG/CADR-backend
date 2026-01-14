using System.Text.Json.Serialization;

namespace Users.Core.Clients.Github.Responses;

internal sealed class GitHubAccessTokenResponse
{
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; init; }

	[JsonPropertyName("token_type")]
	public required string TokenType { get; init; }

	[JsonPropertyName("scope")]
	public required string Scope { get; init; }
}