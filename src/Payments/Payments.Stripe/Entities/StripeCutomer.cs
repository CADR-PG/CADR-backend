using Shared.ValueObjects;

namespace Payments.Stripe.Entities;

public class StripeCutomer
{
	public Guid Id { get; init; }
	public required UserId UserId { get; init; }
	public required string StripeCustomerId { get; init; }
}