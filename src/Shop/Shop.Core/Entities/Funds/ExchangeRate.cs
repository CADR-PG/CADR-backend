namespace Shop.Core.Entities.Funds;

public class ExchangeRate
{
	public Guid Id { get; init; }
	public required string BaseCurrency { get; init; }
	public required string TargetCurrency { get; init; }
	public required decimal Rate { get; init; }
	public required string Provider { get; init; }
	public DateTime ValidFrom { get; init; }
}