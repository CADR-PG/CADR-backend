namespace Shop.Core.Entities.Funds;

public class Currency
{
	public required string Code { get; init; }
	public required string Name { get; init; }
	public required string Symbol { get; init; }

	public int DecimalPlaces { get; init; }
	public bool IsEnabled { get; init; }
}