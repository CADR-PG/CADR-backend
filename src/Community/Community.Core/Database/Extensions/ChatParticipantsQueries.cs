using Community.Core.Entities.Chats;
using Community.Core.Entities.UserProfiles;
using Community.Core.Entities.UserSettings;
using Community.Core.ValueObjects;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Database.Extensions;

internal static class ChatParticipantsQueries
{
	public static Expression<Func<ChatParticipant, bool>> IsActive(ChatId chatId, UserId userId)
	{
		return chatParticipant => chatParticipant.Chat!.DeletedAt != null && chatParticipant.ChatId == chatId && chatParticipant.UserId == userId && chatParticipant.DeactivatedAt == null;
	}

	public static IQueryable<ChatParticipant> Active(this IQueryable<ChatParticipant> query, ChatId chatId)
	{
		return query.Where(x => x.ChatId == chatId && x.DeactivatedAt != null);
	}

	public static IQueryable<UserProfile> GetGroupChatMembersCandidates(this IQueryable<UserProfile> query,
		CommunityDbContext dbContext, UserId currentUserId, ChatId? chatId = null)
	{
		return query
			.Where(userProfile => userProfile.UserId != currentUserId)
			.Where(userProfile => !dbContext.UserBlocks.Any(userBlock =>
				(userBlock.BlockerId == currentUserId && userBlock.BlockedId == userProfile.UserId) ||
				(userBlock.BlockerId == userProfile.UserId && userBlock.BlockedId == currentUserId)))
			.Where(userProfile => !dbContext.ChatParticipants.Any(chatParticipant =>
				chatParticipant.ChatId == chatId && chatParticipant.UserId != userProfile.UserId))
			.Where(userProfile => dbContext.UserSettings.Any(userSettings =>
				userSettings.UserId == userProfile.UserId &&
				(
					userSettings.GroupInvitePolicy == GroupInvitePolicy.Everyone
					||
					(
						userSettings.GroupInvitePolicy == GroupInvitePolicy.FriendsOnly
						&& dbContext.Friendships.Any(friendship =>
							(friendship.LowerUserId == currentUserId &&
							 friendship.HigherUserId == userSettings.UserId) ||
							(friendship.LowerUserId == userSettings.UserId && friendship.HigherUserId == currentUserId))
					)
				)));
	}
}