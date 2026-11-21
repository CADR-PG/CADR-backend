using Community.Core.ValueObjects;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Core.Entities.Chats;

public enum ChatMessageType
{
	User = 0,
	Reply = 10,
	Forward = 20,
	Event = 30
}

public enum ChatMessageEventType
{
	None = 0,
	MessagePinned = 100,
}

internal sealed class ChatMessage
{
	public ChatMessageId Id { get; init; }
	public required ChatMessageType Type { get; init; }
	public Guid? ClientMessageId { get; init; }

	public ChatId ChatId { get; init; }
	public Chat? Chat { get; init; }

	public UserId SenderId { get; init; }
	public ChatParticipant? Sender { get; init; }

	public DateTimeOffset SentAt { get; init; }

	public string? Content { get; private set; }
	public DateTimeOffset? EditedAt { get; private set; }
	public DateTimeOffset? DeletedAt { get; private set; }
	public UserId? DeletedById { get; private set; }

	public ChatMessageId? ReferencedMessageId { get; init; }
	public ChatMessage? ReferencedMessage { get; init; }

	public ChatMessageEventType? EventType { get; init; }

	public static ChatMessage UserMessage(ChatId chatId, UserId userId, Guid clientMessageId, string content, ChatMessageId? replyId, ISnowflakeIdProvider idProvider, TimeProvider timeProvider) => new()
	{
		Id = idProvider.GenerateId<ChatMessageId>(),
		Type = replyId is null ? ChatMessageType.User : ChatMessageType.Reply,
		ClientMessageId = clientMessageId,
		ChatId = chatId,
		SenderId = userId,
		SentAt = timeProvider.GetUtcNow(),
		Content = content,
		ReferencedMessageId = replyId
	};

	public static ChatMessage PinnedEventMessage(ChatMessagePin chatMessagePin, ISnowflakeIdProvider idProvider) => new()
	{
		Id = idProvider.GenerateId<ChatMessageId>(),
		Type = ChatMessageType.Event,
		ClientMessageId = null,
		ChatId = chatMessagePin.ChatId,
		SenderId = chatMessagePin.PinnedById,
		SentAt = chatMessagePin.PinnedAt,
		ReferencedMessageId = chatMessagePin.MessageId,
		EventType = ChatMessageEventType.MessagePinned
	};


	public void Edit(string content, TimeProvider timeProvider)
	{
		if (Type is not (ChatMessageType.User or ChatMessageType.Reply) || DeletedAt is not null) return;

		Content = content;
		EditedAt = timeProvider.GetUtcNow();
	}

	public void MarkAsDeleted(UserId deletedById, TimeProvider timeProvider)
	{
		if (Type is ChatMessageType.Event || DeletedAt is not null) return;

		DeletedById = deletedById;
		DeletedAt = timeProvider.GetUtcNow();
	}
}