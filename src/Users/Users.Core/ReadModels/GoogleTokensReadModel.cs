using System.Text.Json.Serialization;

namespace Users.Core.ReadModels;

public class GoogleTokensReadModel
{
	[JsonPropertyName("id_token")]
	public string? IdToken { get; set; }

	[JsonPropertyName("access_token")]
	public string? AccessToken { get; set; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }
}