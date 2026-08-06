namespace Shop.Core.Entities.Shopping.Cart;

public class CartItem
{
	public Guid Id { get; init; }
	public Guid GameId { get; init; }
	public int Quantity { get; init; }
}