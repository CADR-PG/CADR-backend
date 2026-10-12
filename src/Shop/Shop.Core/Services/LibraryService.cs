using Shared.ValueObjects;
using Shop.Core.Database;
using Shop.Core.Entities.Library;

namespace Shop.Core.Services;

internal sealed class LibraryService(ShopDbContext dbContext, ShopUserService shopUserService)
{
	public async Task GrantAccessToGame(UserId userId, Guid gameId, Guid orderId, CancellationToken cancellationToken)
	{
		await shopUserService.GetOrCreateShopUser(userId, cancellationToken);

		var entry = new LibraryEntry
		{
			Id = Guid.NewGuid(),
			GameId = gameId,
			UserId = userId,
			OrderId = orderId,
			AcquiredAt = DateTime.UtcNow
		};
		await dbContext.LibraryEntries.AddAsync(entry, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);
	}

}