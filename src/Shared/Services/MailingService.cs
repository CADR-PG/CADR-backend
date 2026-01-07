using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Settings;

namespace Shared.Services;

public interface IMailingService
{
	public Task SendPlainAsync(string name, string email, string subject, string body);
	public Task SendHtmlAsync(string name, string email, string subject, string html);
}

internal sealed class MailingService(IOptions<MailingSettings> mailingSettings) : IMailingService
{
	private MailingSettings MailingSettings => mailingSettings.Value;

	public async Task SendPlainAsync(string name, string email, string subject, string body)
	{
		using var message = new MimeMessage();
		message.From.Add(new MailboxAddress(MailingSettings.SenderName, MailingSettings.SmtpEmail));
		message.To.Add(new MailboxAddress(name, email));
		message.Subject = subject;

		message.Body = new TextPart("plain")
		{
			Text = body
		};

		using var client = new SmtpClient();
		await client.ConnectAsync(MailingSettings.SmtpHost, MailingSettings.SmtpPort);
		await client.AuthenticateAsync(MailingSettings.SmtpEmail, MailingSettings.SmtpPassword);
		await client.SendAsync(message);
		await client.DisconnectAsync(true);
	}

	public async Task SendHtmlAsync(string name, string email, string subject, string html)
	{
		using var message = new MimeMessage();
		message.From.Add(new MailboxAddress(MailingSettings.SenderName, MailingSettings.SmtpEmail));
		message.To.Add(new MailboxAddress(name, email));
		message.Subject = subject;

		var builder = new BodyBuilder { HtmlBody = html };
		message.Body = builder.ToMessageBody();

		using var client = new SmtpClient();
		await client.ConnectAsync(MailingSettings.SmtpHost, MailingSettings.SmtpPort);
		await client.AuthenticateAsync(MailingSettings.SmtpEmail, MailingSettings.SmtpPassword);
		await client.SendAsync(message);
		await client.DisconnectAsync(true);
	}
}