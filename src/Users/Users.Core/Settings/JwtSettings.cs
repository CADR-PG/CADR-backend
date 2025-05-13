using Shared.Settings;

namespace Users.Core.Settings;

public class JwtSettings : ISettings
{
	public static string SectionName => "Users:JsonWebTokens";

	public required string AccessSecret { get; set; }
	public required TimeSpan AccessLifetime { get; set; }
	public required string RefreshSecret { get; set; }
	public required TimeSpan RefreshLifetime { get; set; }
	public required string Issuer { get; set; }
	public required string Audience { get; set; }
}