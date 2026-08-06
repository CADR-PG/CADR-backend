

using Shop.Core.Entities.Catalog;

namespace Shop.Core.Entities.Funds;

public class Price
{
	public Guid GameId { get; init; }
	public decimal Amount { get; init; }
	public required string Currency { get; init; }
	public DateTimeOffset ValidFrom { get; init; }
	public string? StripePriceId { get; set; }

	public bool IsFree => Amount == 0;

	public long ToStripeUnitAmount() =>
		(long)Math.Round(Amount * 100, MidpointRounding.AwayFromZero);
}