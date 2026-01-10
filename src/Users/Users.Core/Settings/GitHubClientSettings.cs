using Shared.Settings;
using System.ComponentModel.DataAnnotations;

namespace Users.Core.Settings;

public class GitHubClientSettings : ISettings
{
	public static string SectionName => "Users:OAuthProviders:GitHub";

	[Required]
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }
}