namespace Shared.Settings;

public sealed record MailingSettings(
	string SmtpHost,
	int SmtpPort,
	string SmtpPassword,
	string SmtpEmail,
	string SenderName
) : ISettings
{
	public static string SectionName => "Shared:Mailing";
}