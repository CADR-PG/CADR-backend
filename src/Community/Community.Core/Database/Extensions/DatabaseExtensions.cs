using Community.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.ValueObjects;

namespace Community.Core.Database.Extensions;

internal static class DatabaseExtensions
{
	public static bool IsUniqueViolation(this DbUpdateException exception)
		=> exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

	public static bool IsForeignKeyViolation(this DbUpdateException exception)
		=> exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };

	public static Task LockChatForUpdate(this CommunityDbContext dbContext, ChatId chatId, CancellationToken cancellationToken)
	{
		if (dbContext.Database.CurrentTransaction is null)
			throw new InvalidOperationException("Chat can be locked only inside a transaction.");

		return dbContext.Database.ExecuteSqlAsync(
			$"SELECT 1 FROM \"Community\".\"Chats\" WHERE \"Id\" = {chatId.Value} FOR UPDATE",
			cancellationToken);
	}

	public static Task LockUserProfileForUpdate(this CommunityDbContext dbContext, UserId userId, CancellationToken cancellationToken)
	{
		if (dbContext.Database.CurrentTransaction is null)
			throw new InvalidOperationException("User profile can be locked only inside a transaction.");

		return dbContext.Database.ExecuteSqlAsync(
			$"SELECT 1 FROM \"Community\".\"UserProfiles\" WHERE \"UserId\" = {userId.Value} FOR UPDATE",
			cancellationToken);
	}
}