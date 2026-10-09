using Shared.ValueObjects;
using Shop.Core.Entities.Library;
using Shop.Core.Entities.Shopping.Order;

namespace Shop.Core.Entities.Users;

public class ShopUser
{
	public UserId Id { get; init; }
	public ICollection<LibraryEntry> GamesLibrary { get; init; } = [];
}