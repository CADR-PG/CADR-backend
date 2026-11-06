using Community.Core.Entities.Chats;
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
	public required int UnreadMessagesCount { get; set; }
	public required LastMessageReadModel? LastMessage { get; set; }

	public static Expression<Func<Chat, ChatReadModel>> GetMappingExpression(UserId userId) => chat => new ChatReadModel
	{
		Id = chat.Id,
		Name = chat.Name,
		Type = chat.Type,
		CreatedAt = chat.CreatedAt,
		UnreadMessagesCount = chat.Messages
			.Count(message => message.SenderId != userId && message.SentAt > chat.Participants.First(participant => participant.UserId == userId).LastMessageReadAt),
		LastMessage = chat.Messages
			.OrderByDescending(x => x.SentAt)
			.Select(lastMessage => new LastMessageReadModel
			{
				Content = lastMessage.Content!,
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
		public required string Content { get; set; }
		public required DateTimeOffset SentAt { get; set; }
		public required LastMessageAuthorReadModel Author { get; set; }
	}

	internal sealed class LastMessageAuthorReadModel
	{
		public required Guid Id { get; set; }
		public required string Name { get; set; }
	}
}