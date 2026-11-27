using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Users.Contracts;
using Users.Contracts.ReadModels;
using Users.Core.Database;
using Users.Core.ReadModels;

namespace Users.Core.Services;

internal sealed class UsersProvider(UsersDbContext dbContext) : IUsersProvider
{
	public async Task<UserReadModel?> GetUser(UserId userId, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

		return user?.ToReadModel();
	}

	public async Task<IReadOnlyList<UserId>> GetUserIds(UserId? after, int limit, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

		var query = dbContext.Users.AsNoTracking();

		if (after is { } afterId)
			query = query.Where(user => user.Id > afterId);

		// Index-only scan of the primary key.
		return await query
			.OrderBy(user => user.Id)
			.Take(limit)
			.Select(user => user.Id)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<UserReadModel>> GetUsers(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken)
	{
		if (userIds.Count == 0) return [];

		// Projection - reads only the needed columns.
		return await dbContext.Users
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.Select(user => new UserReadModel(user.Id, user.Email, user.EmailConfirmation.IsConfirmed, user.LastLoggedInAt))
			.ToListAsync(cancellationToken);
	}
}