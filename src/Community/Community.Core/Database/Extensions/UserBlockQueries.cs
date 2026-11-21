using Community.Core.Entities.UserBlocks;
using Shared.ValueObjects;

namespace Community.Core.Database.Extensions;

internal static class UserBlockQueries
{
	public static IQueryable<UserBlock> Between(this IQueryable<UserBlock> query, UserId firstUserId, UserId secondUserId)
	{
		return query.Where(userBlock =>
			(userBlock.BlockerId == firstUserId && userBlock.BlockedId == secondUserId) ||
			(userBlock.BlockerId == secondUserId && userBlock.BlockedId == firstUserId));
	}
}
