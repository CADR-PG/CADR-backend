using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Shop.Core.Database;
using Shop.Core.Entities.Library;
using Shop.Core.Entities.Users;

namespace Shop.Core.Services;

internal sealed class LibraryService(ShopDbContext dbContext)
{
	public async Task GrantAccessToGame(UserId userId, Guid gameId, Guid orderId, CancellationToken cancellationToken)
	{
		var shopUserExists = await dbContext.ShopUsers.AnyAsync(u => u.Id == userId, cancellationToken);
		if (!shopUserExists)
			await dbContext.ShopUsers.AddAsync(new ShopUser { Id = userId }, cancellationToken);

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