using Shared.ValueObjects;

namespace Shop.Core.Entities.Shopping.Cart;

public class Cart
{
	public Guid Id { get; init; }
	public UserId UserId { get; init; }
	public ICollection<CartItem> Items { get; } = [];
}