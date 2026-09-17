using Shared.ValueObjects;
using Shop.Core.Entities.Payments;

namespace Shop.Core.Entities.Funds;

public class Wallets
{
	public Guid Id { get; init; }
	public UserId UserId { get; init; }
	public long? Ballance { get; set; }
	public ICollection<Transactions> Transactions { get; init; } = [];

	public static Wallets Create(UserId userId) => new() { Id = Guid.NewGuid(), UserId = userId, Ballance = 0 };
}