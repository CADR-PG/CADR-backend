using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Models.Chats;

internal sealed class ChatParticipantReadStateReadModel
{
	public required UserId ParticipantId { get; init; }
	public required ChatMessageId? LastReadMessageId { get; init; }
	public required DateTimeOffset? LastMessageReadAt { get; init; }

	public static Expression<Func<ChatParticipant, ChatParticipantReadStateReadModel>> MappingExpression => chatParticipant => new ChatParticipantReadStateReadModel
	{
		ParticipantId = chatParticipant.UserId,
		LastReadMessageId = chatParticipant.LastReadMessageId,
		LastMessageReadAt = chatParticipant.LastMessageReadAt,
	};
}