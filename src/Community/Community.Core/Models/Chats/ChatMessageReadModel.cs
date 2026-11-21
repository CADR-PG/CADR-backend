using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Models.Chats;

internal sealed class ChatMessageReadModel
{
	public required ChatMessageId Id { get; set; }
	public required ChatMessageType Type { get; set; }
	public required ChatMessageEventType? EventType { get; set; }
	public required string? Content { get; set; }
	public required UserId SenderId { get; set; }
	public required DateTimeOffset SentAt { get; set; }
	public required DateTimeOffset? EditedAt { get; set; }
	public required DateTimeOffset? DeletedAt { get; set; }

	public required ReferencedMessageReadModel? ReferencedMessage { get; set; }

	internal sealed class ReferencedMessageReadModel
	{
		public required ChatMessageId Id { get; init; }
		public ChatMessageType Type { get; init; }
		public required UserId SenderId { get; init; }
		public required string? Content { get; init; }
	}

	public static Expression<Func<ChatMessage, ChatMessageReadModel>> MappingExpression { get; } = chatMessage => new ChatMessageReadModel
	{
		Id = chatMessage.Id,
		Type = chatMessage.Type,
		EventType = chatMessage.EventType,
		Content = chatMessage.DeletedAt == null ? chatMessage.Content : null,
		SenderId = chatMessage.SenderId,
		SentAt = chatMessage.SentAt,
		EditedAt = chatMessage.EditedAt,
		DeletedAt = chatMessage.DeletedAt,
		ReferencedMessage = chatMessage.ReferencedMessageId == null ? null : new ReferencedMessageReadModel
		{
			Id = chatMessage.ReferencedMessage!.Id,
			Type = chatMessage.ReferencedMessage.Type,
			SenderId = chatMessage.ReferencedMessage.SenderId,
			Content = chatMessage.ReferencedMessage.DeletedAt == null ? chatMessage.ReferencedMessage.Content : null
		}
	};

	public static Func<ChatMessage, ChatMessageReadModel> Map { get; } = MappingExpression.Compile();
}