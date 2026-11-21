using Community.Core.Entities.UserProfiles;
using Community.Core.ValueObjects;
using Shared.ValueObjects;

namespace Community.Core.Entities.Chats;

internal enum ChatParticipantRole
{
	Member = 0,
	Admin = 10
}

internal sealed class ChatParticipant
{
	public UserId UserId { get; private init; }
	public UserProfile? UserProfile { get; private init; }
	public string? Name { get; private set; }
	public ChatParticipantRole Role { get; private set; }
	public DateTimeOffset? JoinedAt { get; private set; }
	public ChatMessageId? LastReadMessageId { get; private set; }
	public DateTimeOffset? LastMessageReadAt { get; private set; }

	public ChatId ChatId { get; private init; }
	public Chat? Chat { get; private init; }

	public DateTimeOffset? DeactivatedAt { get; private set; }

	private ChatParticipant() { }

	public static ChatParticipant CreateParticipant(ChatId chatId, UserId participantId, DateTimeOffset joinedAt) => new()
	{
		UserId = participantId,
		Name = null,
		JoinedAt = joinedAt,
		ChatId = chatId,
		Role = ChatParticipantRole.Member,
	};

	public static ChatParticipant CreateAdmin(ChatId chatId, UserId ownerId, DateTimeOffset joinedAt) => new()
	{
		UserId = ownerId,
		Name = null,
		ChatId = chatId,
		JoinedAt = joinedAt,
		Role = ChatParticipantRole.Admin
	};

	public void Deactivate(TimeProvider timeProvider)
	{
		if (DeactivatedAt is not null) return;

		DeactivatedAt = timeProvider.GetUtcNow();
		Role = ChatParticipantRole.Member;
	}

	public void Reactivate(DateTimeOffset joinedAt)
	{
		if (DeactivatedAt is null) return;

		DeactivatedAt = null;
		JoinedAt = joinedAt;
		Role = ChatParticipantRole.Member;
	}

	public void PromoteToAdmin()
	{
		Role = ChatParticipantRole.Admin;
	}

	public void DemoteToMember()
	{
		Role = ChatParticipantRole.Member;
	}

	public bool MarkAsRead(ChatMessageId messageId, TimeProvider timeProvider)
	{
		if (LastReadMessageId is { } lastReadMessageId && messageId <= lastReadMessageId) return false;

		LastReadMessageId = messageId;
		LastMessageReadAt = timeProvider.GetUtcNow();
		return true;
	}
}