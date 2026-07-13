namespace Shop.Core.Entities.Funds;

public sealed class Money
{
	public decimal Amount { get; init; }
	public required string Currency { get; init; }

	public static Money Of(decimal amount, string currency) =>
		new() { Amount = amount, Currency = currency.ToUpperInvariant() };

}