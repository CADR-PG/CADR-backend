using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Shop.Core.Database;
using Shop.Core.Entities.Funds;
using Shop.Core.Entities.Users;

namespace Shop.Core.Services;

internal sealed class ShopUserService(ShopDbContext dbContext)
{
	public async Task<ShopUser> GetOrCreateShopUser(UserId userId, CancellationToken cancellationToken)
	{
		var shopUser = await dbContext.ShopUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
		if (shopUser is null)
		{
			shopUser = new ShopUser { Id = userId };
			await dbContext.ShopUsers.AddAsync(shopUser, cancellationToken);
		}

		var hasWallet = await dbContext.Wallets.AnyAsync(w => w.UserId == userId, cancellationToken);
		if (!hasWallet)
			await dbContext.Wallets.AddAsync(Wallets.Create(userId), cancellationToken);

		return shopUser;
	}
}