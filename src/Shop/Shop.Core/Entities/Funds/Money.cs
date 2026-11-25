using Shop.Core.Enums;

namespace Shop.Core.Entities.Funds;

public sealed class Money
{
	public long Amount { get; init; }
	public required Currency Currency { get; init; }
}