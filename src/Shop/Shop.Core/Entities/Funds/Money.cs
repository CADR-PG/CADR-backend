using Shop.Core.Enums;

namespace Shop.Core.Entities.Funds;

public sealed class Money
{
	// TODO: money docelowo ma byc encja która utworzy wirtualny nominał, to znaczy kwota plus waluta,
	//		taki banknot w obrebie systemu
	public decimal Amount { get; init; }
	public required Currency Currency { get; init; }
}