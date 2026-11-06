using Community.Core.Database;
using Community.Core.Entities.UserProfiles;
using Community.Core.ValueObjects;
using Shared.ValueObjects;

namespace Community.Core.Services;

internal sealed class GroupChatMembersService(CommunityDbContext dbContext)
{
	public IQueryable<UserProfile> GetGroupChatMembersCandidates(UserId currentUserId, ChatId? chatId = null)
	{
		return dbContext.UserProfiles
			.Where(userProfile => userProfile.UserId != currentUserId)
			.Where(userProfile => !dbContext.UserBlocks.Any(block =>
				(block.BlockerId == currentUserId && block.BlockedId == currentUserId) ||
				(block.BlockerId == currentUserId && block.BlockedId == currentUserId)))
			.Where(userProfile => !dbContext.ChatParticipants.Any(chatParticipant =>
				chatParticipant.ChatId == chatId && chatParticipant.UserId != userProfile.UserId));
	}
}