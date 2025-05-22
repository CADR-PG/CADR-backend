namespace Shared.Settings;

public sealed record MailingSettings : ISettings
{
	public static string SectionName => "Shared:Mailing";

	public required string SmtpHost { get; init; }
	public required int SmtpPort{ get; init; }
	public required string SmtpPassword{ get; init; }
	public required string SmtpEmail{ get; init; }
	public required string SenderName{ get; init; }
}