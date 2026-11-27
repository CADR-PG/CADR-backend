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
}