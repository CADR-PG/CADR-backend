using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Community.Tests.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using Shared.ValueObjects;

namespace Community.Tests.Unit;

public sealed class ChatMessageTests
{
	private static readonly ChatId Chat = new(42);
	private static readonly UserId Author = new(Guid.NewGuid());
	private static readonly UserId Other = new(Guid.NewGuid());

	private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
	private readonly SequentialSnowflakeIdProvider _ids = new();

	private ChatMessage NewMessage(ChatMessageId? replyTo = null)
		=> ChatMessage.UserMessage(Chat, Author, Guid.NewGuid(), "hello", replyTo, _ids, _clock);

	private ChatMessage NewEventMessage()
	{
		var pin = ChatMessagePin.Create(Chat, new ChatMessageId(7), Other, _clock);
		return ChatMessage.PinnedEventMessage(pin, _ids);
	}

	[Fact]
	public void UserMessageWithoutReplyHasUserType()
	{
		var message = NewMessage();

		Assert.Equal(ChatMessageType.User, message.Type);
		Assert.Null(message.ReferencedMessageId);
		Assert.Equal("hello", message.Content);
		Assert.Equal(_clock.GetUtcNow(), message.SentAt);
	}

	[Fact]
	public void UserMessageWithReplyHasReplyTypeAndReference()
	{
		var message = NewMessage(new ChatMessageId(5));

		Assert.Equal(ChatMessageType.Reply, message.Type);
		Assert.Equal(new ChatMessageId(5), message.ReferencedMessageId);
	}

	[Fact]
	public void PinnedEventMessageReferencesPinnedMessage()
	{
		var pin = ChatMessagePin.Create(Chat, new ChatMessageId(7), Other, _clock);

		var message = ChatMessage.PinnedEventMessage(pin, _ids);

		Assert.Equal(ChatMessageType.Event, message.Type);
		Assert.Equal(ChatMessageEventType.MessagePinned, message.EventType);
		Assert.Equal(new ChatMessageId(7), message.ReferencedMessageId);
		Assert.Equal(Other, message.SenderId);
		Assert.Equal(pin.PinnedAt, message.SentAt);
		Assert.Null(message.Content);
		Assert.Null(message.ClientMessageId);
	}

	[Fact]
	public void EditChangesContentAndSetsEditedAt()
	{
		var message = NewMessage();
		_clock.Advance(TimeSpan.FromMinutes(5));

		message.Edit("changed", _clock);

		Assert.Equal("changed", message.Content);
		Assert.Equal(_clock.GetUtcNow(), message.EditedAt);
	}

	[Fact]
	public void EditIsIgnoredForDeletedMessage()
	{
		var message = NewMessage();
		message.MarkAsDeleted(Author, _clock);

		message.Edit("changed", _clock);

		Assert.Equal("hello", message.Content);
		Assert.Null(message.EditedAt);
	}

	[Fact]
	public void EditIsIgnoredForEventMessage()
	{
		var message = NewEventMessage();

		message.Edit("changed", _clock);

		Assert.Null(message.Content);
		Assert.Null(message.EditedAt);
	}

	[Fact]
	public void MarkAsDeletedKeepsFirstDeletion()
	{
		var message = NewMessage();
		var firstDeletionAt = _clock.GetUtcNow();

		message.MarkAsDeleted(Author, _clock);
		_clock.Advance(TimeSpan.FromMinutes(1));
		message.MarkAsDeleted(Other, _clock);

		Assert.Equal(firstDeletionAt, message.DeletedAt);
		Assert.Equal(Author, message.DeletedById);
	}

	[Fact]
	public void MarkAsDeletedIsIgnoredForEventMessage()
	{
		var message = NewEventMessage();

		message.MarkAsDeleted(Author, _clock);

		Assert.Null(message.DeletedAt);
	}
}