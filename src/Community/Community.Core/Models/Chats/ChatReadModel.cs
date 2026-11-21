using Community.Core.Database;
using Community.Core.Entities.Chats;
using Community.Core.Models.Profiles;
using Community.Core.ValueObjects;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Models.Chats;

internal sealed record ChatReadModel
{
	public required ChatId Id { get; set; }
	public required string? Name { get; set; }
	public required ChatType Type { get; set; }
	public required DateTimeOffset CreatedAt { get; set; }

	public required UserProfileSummaryReadModel? Counterpart { get; set; }

	public required bool IsReadOnly { get; set; }

	public required ChatMessageId? LastReadMessageId { get; set; }
	public required int UnreadMessagesCount { get; set; }
	public required LastMessageReadModel? LastMessage { get; set; }

	public static Expression<Func<Chat, ChatReadModel>> GetMappingExpression(UserId userId, CommunityDbContext dbContext) => chat => new ChatReadModel
	{
		Id = chat.Id,
		Name = chat.Name,
		Type = chat.Type,
		CreatedAt = chat.CreatedAt,
		Counterpart = chat.Type == ChatType.Direct
			? chat.Participants
				.Where(participant => participant.UserId != userId)
				.Select(participant => new UserProfileSummaryReadModel
				{
					UserId = participant.UserId,
					Name = participant.Name ?? participant.UserProfile!.Name
				})
				.FirstOrDefault()
			: null,
		IsReadOnly = chat.Type == ChatType.Direct
					 && (!dbContext.Friendships.Any(friendship =>
							 friendship.LowerUserId == chat.DirectChatLowerUserId!.Value
							 && friendship.HigherUserId == chat.DirectChatHigherUserId!.Value)
						 || dbContext.UserBlocks.Any(userBlock =>
							 (userBlock.BlockerId == chat.DirectChatLowerUserId!.Value && userBlock.BlockedId == chat.DirectChatHigherUserId!.Value)
							 || (userBlock.BlockerId == chat.DirectChatHigherUserId!.Value && userBlock.BlockedId == chat.DirectChatLowerUserId!.Value))),
		LastReadMessageId = chat.Participants
			.Where(participant => participant.UserId == userId)
			.Select(participant => participant.LastReadMessageId)
			.FirstOrDefault(),
		UnreadMessagesCount = chat.Messages
			.Count(message => message.SenderId != userId
							  && message.DeletedAt == null
							  && chat.Participants
								  .Where(participant => participant.UserId == userId)
								  .All(participant => participant.LastReadMessageId == null || message.Id > participant.LastReadMessageId.Value)),
		LastMessage = chat.Messages
			.OrderByDescending(x => x.Id)
			.Select(lastMessage => new LastMessageReadModel
			{
				Id = lastMessage.Id,
				Content = lastMessage.DeletedAt == null ? lastMessage.Content : null,
				SentAt = lastMessage.SentAt,
				Author = new LastMessageAuthorReadModel
				{
					Id = lastMessage.SenderId,
					Name = lastMessage.Sender!.Name ?? lastMessage.Sender.UserProfile!.Name
				}
			})
			.FirstOrDefault()
	};

	internal sealed class LastMessageReadModel
	{
		public required ChatMessageId Id { get; set; }
		public required string? Content { get; set; }
		public required DateTimeOffset SentAt { get; set; }
		public required LastMessageAuthorReadModel Author { get; set; }
	}

	internal sealed class LastMessageAuthorReadModel
	{
		public required UserId Id { get; set; }
		public required string Name { get; set; }
	}
}