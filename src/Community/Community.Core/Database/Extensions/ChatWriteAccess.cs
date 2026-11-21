using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints.Results;
using System.Linq.Expressions;

namespace Community.Core.Database.Extensions;

internal static class ChatWriteAccess
{
	public const string ReadOnlyErrorCode = "Chat.ReadOnly";

	public static ErrorResult ReadOnlyError()
		=> new(ReadOnlyErrorCode, "This chat is read-only. Renew the friendship to write again.", 403);

	public static Expression<Func<Chat, bool>> IsReadOnly(CommunityDbContext dbContext)
	{
		return chat => chat.Type == ChatType.Direct
					   && (!dbContext.Friendships.Any(friendship =>
							   friendship.LowerUserId == chat.DirectChatLowerUserId!.Value
							   && friendship.HigherUserId == chat.DirectChatHigherUserId!.Value)
						   || dbContext.UserBlocks.Any(userBlock =>
							   (userBlock.BlockerId == chat.DirectChatLowerUserId!.Value && userBlock.BlockedId == chat.DirectChatHigherUserId!.Value)
							   || (userBlock.BlockerId == chat.DirectChatHigherUserId!.Value && userBlock.BlockedId == chat.DirectChatLowerUserId!.Value)));
	}

	public static Task<bool> IsChatReadOnly(this CommunityDbContext dbContext, ChatId chatId, CancellationToken cancellationToken)
	{
		return dbContext.Chats
			.Where(chat => chat.Id == chatId)
			.Where(IsReadOnly(dbContext))
			.AnyAsync(cancellationToken);
	}
}