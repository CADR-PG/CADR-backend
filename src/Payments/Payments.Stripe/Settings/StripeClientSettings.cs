using Shared.Settings;
using System.ComponentModel.DataAnnotations;

namespace Payments.Stripe.Settings;

public class StripeClientSettings : ISettings
{
	public static string SectionName => "Payments:Stripe";
	[Required]
	public required string SecretApiKey { get; set; }
	public required string PublishableApiKey { get; set; }
	public required string WebhookSecret { get; set; }
}