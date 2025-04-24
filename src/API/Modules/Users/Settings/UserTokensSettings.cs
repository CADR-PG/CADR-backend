using API.Shared.Settings;

namespace API.Modules.Users.Settings;

internal sealed class UserTokensSettings : ISettings
{
	public static string SectionName => "Users:Tokens";

	public required string AccessSecret { get; set; }
	public required TimeSpan AccessLifetime { get; set; }
	public required string RefreshSecret { get; set; }
	public required TimeSpan RefreshLifetime { get; set; }
	public required string Issuer { get; set; }
	public required string Audience { get; set; }
}