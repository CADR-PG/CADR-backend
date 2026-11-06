using Community.Core.ValueObjects;
using Shared.ValueObjects;

namespace Community.Core.Entities.Chats;

internal sealed class ChatMessagePin
{
	public ChatId ChatId { get; init; }
	public ChatMessageId MessageId { get; init; }
	public ChatMessage? Message { get; init; }
	public UserId PinnedById { get; init; }
	public DateTimeOffset PinnedAt { get; init; }

	public static ChatMessagePin Create(ChatId chatId, ChatMessageId messageId, UserId pinnedById, TimeProvider timeProvider)
	{
		return new ChatMessagePin
		{
			ChatId = chatId,
			MessageId = messageId,
			PinnedById = pinnedById,
			PinnedAt = timeProvider.GetUtcNow()
		};
	}
}