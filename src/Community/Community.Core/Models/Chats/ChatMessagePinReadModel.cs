using Community.Core.Entities.Chats;
using Shared.ValueObjects;
using System.Linq.Expressions;

namespace Community.Core.Models.Chats;

internal sealed class ChatMessagePinReadModel
{
	public required UserId PinnedBy { get; set; }
	public required DateTimeOffset PinnedAt  { get; set; }
	public required ChatMessageReadModel Message { get; set; }

	public static Expression<Func<ChatMessagePin, ChatMessagePinReadModel>> MappingExpression { get; } = chatMessagePin => new ChatMessagePinReadModel
	{
		PinnedBy = chatMessagePin.PinnedById,
		PinnedAt = chatMessagePin.PinnedAt,
		Message = new ChatMessageReadModel
		{
			Id = chatMessagePin.Message!.Id,
			Type = chatMessagePin.Message.Type,
			EventType = chatMessagePin.Message.EventType,
			Content = chatMessagePin.Message.DeletedAt == null ? chatMessagePin.Message.Content : null,
			SenderId = chatMessagePin.Message.SenderId,
			SentAt = chatMessagePin.Message.SentAt,
			EditedAt = chatMessagePin.Message.EditedAt,
			DeletedAt = chatMessagePin.Message.DeletedAt,
			ReferencedMessage = chatMessagePin.Message.ReferencedMessageId == null ? null : new ChatMessageReadModel.ReferencedMessageReadModel
			{
				Id = chatMessagePin.Message.ReferencedMessage!.Id,
				Type = chatMessagePin.Message.ReferencedMessage.Type,
				SenderId = chatMessagePin.Message.ReferencedMessage.SenderId,
				Content = chatMessagePin.Message.ReferencedMessage.DeletedAt == null ? chatMessagePin.Message.ReferencedMessage.Content : null
			}
		}
	};

	public static Func<ChatMessagePin, ChatMessagePinReadModel> Map { get; } = MappingExpression.Compile();
}