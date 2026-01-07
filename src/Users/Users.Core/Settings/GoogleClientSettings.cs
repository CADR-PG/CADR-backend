using Shared.Settings;
using System.ComponentModel.DataAnnotations;

namespace Users.Core.Settings;

public class GoogleClientSettings : ISettings
{
	public static string SectionName => "Users:OAuthProviders:Google";

	[Required]
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }

	public required Uri RedirectUri { get; set; }
}