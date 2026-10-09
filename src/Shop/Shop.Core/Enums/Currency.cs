namespace Shop.Core.Enums;

public enum Currency
{
	Zloty = 0, // default currency
	Dollar,
	Euro,
}

public static class CurrencyExtensions
{
	public static string ToSymbol(this Currency currency) => currency switch
	{
		Currency.Zloty => "zł",
		Currency.Dollar => "$",
		Currency.Euro => "€",
		_ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
	};
}