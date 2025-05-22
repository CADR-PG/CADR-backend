using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Settings;

namespace Shared.Services;

public interface IMailingService
{
	public Task SendAsync(string name, string email, string subject, string body);
}

internal sealed class MailingService(IOptions<MailingSettings> mailingSettings) : IMailingService
{
	private MailingSettings MailingSettings => mailingSettings.Value;

	public async Task SendAsync(string name, string email, string subject, string body)
	{
		using var message = new MimeMessage();
		message.From.Add(new MailboxAddress(MailingSettings.SenderName, MailingSettings.SmtpEmail));
		message.To.Add(new MailboxAddress(name, email));
		message.Subject = subject;

		message.Body = new TextPart ("plain") {
			Text = body
		};

		using var client = new SmtpClient();
		await client.ConnectAsync(MailingSettings.SmtpHost, MailingSettings.SmtpPort, true);
		await client.AuthenticateAsync(MailingSettings.SmtpEmail, MailingSettings.SmtpPassword);
		await client.SendAsync(message);
		await client.DisconnectAsync(true);
	}
}