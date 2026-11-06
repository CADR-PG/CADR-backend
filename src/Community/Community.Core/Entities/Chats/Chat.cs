using Community.Core.ValueObjects;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Core.Entities.Chats;

internal enum ChatType
{
	Direct = 0,
	Group = 10,
	Project = 100
}

internal sealed class Chat
{
	public ChatId Id { get; private init; }
	public string? Name { get; private set; }
	public UserId? DirectChatLowerUserId { get; private set; }
	public UserId? DirectChatHigherUserId { get; private set; }
	public ChatType Type { get; private init; }

	public DateTimeOffset CreatedAt { get; private init; }

	public List<ChatParticipant> Participants { get; private set; } = [];
	public List<ChatMessage> Messages { get; private set; } = [];

	public DateTimeOffset? DeletedAt { get; private set; }
	public UserId? DeletedBy { get; private set; }

	public static Chat CreateDirectChat(UserId firstUserId, UserId secondUserId, ISnowflakeIdProvider idProvider, TimeProvider timeProvider)
	{
		var (lowerUserId, higherUserId) = UserId.GetLowerAndHigherUserId(firstUserId, secondUserId);

		var createdAt = timeProvider.GetUtcNow();
		var chatId = idProvider.GenerateId<ChatId>();
		return new Chat
		{
			Id = chatId,
			Name = null,
			DirectChatLowerUserId = lowerUserId,
			DirectChatHigherUserId = higherUserId,
			Type = ChatType.Direct,
			CreatedAt = createdAt,
			Participants =
			[
				ChatParticipant.CreateParticipant(chatId, lowerUserId, createdAt),
				ChatParticipant.CreateParticipant(chatId, higherUserId, createdAt),
			],
			Messages = []
		};
	}

	public static Chat CreateGroupChat(string name, UserId ownerId, IEnumerable<UserId> participantIds, ISnowflakeIdProvider idProvider, TimeProvider timeProvider)
	{
		var chatId = idProvider.GenerateId<ChatId>();
		var createdAt = timeProvider.GetUtcNow();
		return new Chat
		{
			Id = chatId,
			Name = name.Trim(),
			DirectChatLowerUserId = null,
			DirectChatHigherUserId = null,
			Type = ChatType.Direct,
			CreatedAt = default,
			Participants =
			[
				ChatParticipant.CreateAdmin(chatId, ownerId, createdAt), .. participantIds
					.Where(participantId => participantId != ownerId)
					.Select(participantId => ChatParticipant.CreateParticipant(chatId, participantId, createdAt))
			],
			Messages = []
		};
	}

	public void ChangeName(string name)
	{
		if (Type == ChatType.Group)
			Name = name;
	}

	public void MarkAsDeleted(UserId deletedBy, TimeProvider timeProvider)
	{
		if (Type != ChatType.Group) return;

		DeletedBy = deletedBy;
		DeletedAt = timeProvider.GetUtcNow();
	}
}