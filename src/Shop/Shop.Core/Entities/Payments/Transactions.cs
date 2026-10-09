using Shared.ValueObjects;

namespace Shop.Core.Entities.Payments;

public class Transactions
{
	public Guid TransactionId { get; init; }
	public UserId UserId { get; init; }
	public Guid OrderId { get; init; }
	public decimal Amount { get; init; }
	public TransactionTypes TransactionType { get; init; }
	public DateTime TransactionDate { get; init; }
}