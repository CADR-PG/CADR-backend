using Community.Core.Entities.Chats;
using Community.Core.Features.ChatMessages;
using Community.Core.Features.Chats;
using Community.Core.Models.Chats;
using Community.Core.ValueObjects;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class ChatMessageTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	private UserId _alice;
	private UserId _bob;
	private ChatId _chat;

	private async Task GivenDirectChat()
	{
		_alice = await Host.CreateUser("Alice");
		_bob = await Host.CreateUser("Bob");
		await MakeFriends(_alice, _bob);
		_chat = await StartDirectChat(_alice, _bob);
	}

	[Fact]
	public async Task SendingTrimsContentAndMarksMessageReadBySender()
	{
		await GivenDirectChat();

		var message = await SendMessage(_alice, _chat, "  hello  ");

		Assert.Equal("hello", message.Content);
		Assert.Equal(ChatMessageType.User, message.Type);
		var readStates = ResultAssert.Ok<Listed<ChatParticipantReadStateReadModel>>(await Host.Send(new GetChatReadStates(As(_bob), _chat)));
		Assert.Equal(message.Id, readStates.Items.Single(state => state.ParticipantId == _alice).LastReadMessageId);
		Assert.Null(readStates.Items.Single(state => state.ParticipantId == _bob).LastReadMessageId);
	}

	[Fact]
	public async Task WhitespaceOnlyContentIsRejected()
	{
		await GivenDirectChat();

		var result = await Host.Send(new SendChatMessage(As(_alice), _chat, new SendChatMessage.Body(Guid.NewGuid(), "   ", null)));

		ResultAssert.Error(result, "Message.EmptyContent", 400);
	}

	[Fact]
	public async Task EmptyClientMessageIdIsRejected()
	{
		await GivenDirectChat();

		var result = await Host.Send(new SendChatMessage(As(_alice), _chat, new SendChatMessage.Body(Guid.Empty, "hi", null)));

		ResultAssert.Error(result, "Message.InvalidClientMessageId", 400);
	}

	[Fact]
	public async Task ResendingWithSameClientMessageIdReturnsOriginalMessage()
	{
		await GivenDirectChat();
		var clientMessageId = Guid.NewGuid();
		var first = ResultAssert.Created<ChatMessageReadModel>(
			await Host.Send(new SendChatMessage(As(_alice), _chat, new SendChatMessage.Body(clientMessageId, "hi", null))));

		var retry = ResultAssert.Ok<ChatMessageReadModel>(
			await Host.Send(new SendChatMessage(As(_alice), _chat, new SendChatMessage.Body(clientMessageId, "hi (retry)", null))));

		Assert.Equal(first.Id, retry.Id);
		Assert.Equal("hi", retry.Content);
		Assert.Equal(1, await Host.Query(db => db.ChatMessages.CountAsync(m => m.ChatId == _chat)));
	}

	[Fact]
	public async Task SameClientMessageIdFromDifferentSendersCreatesSeparateMessages()
	{
		await GivenDirectChat();
		var clientMessageId = Guid.NewGuid();

		var fromAlice = ResultAssert.Created<ChatMessageReadModel>(
			await Host.Send(new SendChatMessage(As(_alice), _chat, new SendChatMessage.Body(clientMessageId, "a", null))));
		var fromBob = ResultAssert.Created<ChatMessageReadModel>(
			await Host.Send(new SendChatMessage(As(_bob), _chat, new SendChatMessage.Body(clientMessageId, "b", null))));

		Assert.NotEqual(fromAlice.Id, fromBob.Id);
	}

	[Fact]
	public async Task ReplyReferencesOriginalMessage()
	{
		await GivenDirectChat();
		var original = await SendMessage(_alice, _chat, "question");

		var reply = await SendMessage(_bob, _chat, "answer", original.Id);

		Assert.Equal(ChatMessageType.Reply, reply.Type);
		Assert.Equal(original.Id, reply.ReferencedMessage!.Id);
		Assert.Equal("question", reply.ReferencedMessage.Content);
	}

	[Fact]
	public async Task ReplyToMessageFromAnotherChatIsRejected()
	{
		await GivenDirectChat();
		var carol = await Host.CreateUser("Carol");
		await MakeFriends(_alice, carol);
		var otherChat = await StartDirectChat(_alice, carol);
		var foreignMessage = await SendMessage(_alice, otherChat);

		var result = await Host.Send(new SendChatMessage(As(_bob), _chat, new SendChatMessage.Body(Guid.NewGuid(), "re", foreignMessage.Id)));

		ResultAssert.Error(result, "Message.InvalidReply", 400);
	}

	[Fact]
	public async Task ReplyToDeletedMessageIsRejected()
	{
		await GivenDirectChat();
		var original = await SendMessage(_alice, _chat);
		ResultAssert.NoContent(await Host.Send(new DeleteChatMessage(As(_alice), _chat, original.Id)));

		var result = await Host.Send(new SendChatMessage(As(_bob), _chat, new SendChatMessage.Body(Guid.NewGuid(), "re", original.Id)));

		ResultAssert.Error(result, "Message.InvalidReply", 400);
	}

	[Fact]
	public async Task AuthorCanEditOwnMessage()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat, "typo");
		Host.Clock.Advance(TimeSpan.FromMinutes(1));

		var edited = ResultAssert.Ok<ChatMessageReadModel>(
			await Host.Send(new EditChatMessage(As(_alice), _chat, message.Id, new EditChatMessage.Body("  fixed "))));

		Assert.Equal("fixed", edited.Content);
		Assert.Equal(Host.Clock.GetUtcNow(), edited.EditedAt);
	}

	[Fact]
	public async Task OtherParticipantCannotEditMessage()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat, "original");

		var result = await Host.Send(new EditChatMessage(As(_bob), _chat, message.Id, new EditChatMessage.Body("hacked")));

		ResultAssert.Error(result, "ChatMessage.NotFound", 404);
		Assert.Equal("original", await Host.Query(db => db.ChatMessages.Where(m => m.Id == message.Id).Select(m => m.Content).SingleAsync()));
	}

	[Fact]
	public async Task OtherParticipantCannotDeleteMessageInDirectChat()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat);

		ResultAssert.Error(await Host.Send(new DeleteChatMessage(As(_bob), _chat, message.Id)), "ChatMessage.Forbidden", 403);
	}

	[Fact]
	public async Task DeletingMessageSoftDeletesItAndRemovesItsPin()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat);
		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(_bob), _chat, message.Id)));

		ResultAssert.NoContent(await Host.Send(new DeleteChatMessage(As(_alice), _chat, message.Id)));
		ResultAssert.NoContent(await Host.Send(new DeleteChatMessage(As(_alice), _chat, message.Id)));

		var stored = await Host.Query(db => db.ChatMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id));
		Assert.NotNull(stored.DeletedAt);
		Assert.Equal(_alice, stored.DeletedById);
		Assert.False(await Host.Query(db => db.ChatMessagePins.AnyAsync(p => p.MessageId == message.Id)));
		ResultAssert.Error(
			await Host.Send(new EditChatMessage(As(_alice), _chat, message.Id, new EditChatMessage.Body("zombie"))),
			"ChatMessage.NotFound", 404);
	}

	[Fact]
	public async Task PinningCreatesPinAndEventMessageOnce()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat);

		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(_bob), _chat, message.Id)));
		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(_bob), _chat, message.Id)));

		var pins = ResultAssert.Ok<CursorPaginated<ChatMessagePinReadModel>>(await Host.Send(new GetPinnedMessages(As(_alice), _chat, null, null, null)));
		var pin = Assert.Single(pins.Items);
		Assert.Equal(message.Id, pin.Message.Id);
		Assert.Equal(_bob, pin.PinnedBy);
		var events = await Host.Query(db => db.ChatMessages.AsNoTracking().Where(m => m.ChatId == _chat && m.Type == ChatMessageType.Event).ToListAsync());
		var pinEvent = Assert.Single(events);
		Assert.Equal(ChatMessageEventType.MessagePinned, pinEvent.EventType);
		Assert.Equal(message.Id, pinEvent.ReferencedMessageId);
	}

	[Fact]
	public async Task EventMessageCannotBePinnedEditedOrDeleted()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat);
		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(_alice), _chat, message.Id)));
		var eventId = await Host.Query(db => db.ChatMessages.Where(m => m.ChatId == _chat && m.Type == ChatMessageType.Event).Select(m => m.Id).SingleAsync());

		ResultAssert.Error(await Host.Send(new PinChatMessage(As(_alice), _chat, eventId)), "PinChatMessage.InvalidMessage", 400);
		ResultAssert.Error(
			await Host.Send(new EditChatMessage(As(_alice), _chat, eventId, new EditChatMessage.Body("x"))),
			"ChatMessage.Invalid", 400);
		ResultAssert.Error(await Host.Send(new DeleteChatMessage(As(_alice), _chat, eventId)), "ChatMessage.Invalid", 400);
	}

	[Fact]
	public async Task UnpinningRemovesPin()
	{
		await GivenDirectChat();
		var message = await SendMessage(_alice, _chat);
		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(_alice), _chat, message.Id)));

		ResultAssert.NoContent(await Host.Send(new UnpinChatMessage(As(_bob), _chat, message.Id)));

		var pins = ResultAssert.Ok<CursorPaginated<ChatMessagePinReadModel>>(await Host.Send(new GetPinnedMessages(As(_alice), _chat, null, null, null)));
		Assert.Empty(pins.Items);
	}

	[Fact]
	public async Task ReadMarkerOnlyMovesForward()
	{
		await GivenDirectChat();
		var first = await SendMessage(_alice, _chat, "1");
		var second = await SendMessage(_alice, _chat, "2");

		ResultAssert.NoContent(await Host.Send(new MarkChatAsRead(As(_bob), _chat, new MarkChatAsRead.Body(second.Id))));
		ResultAssert.NoContent(await Host.Send(new MarkChatAsRead(As(_bob), _chat, new MarkChatAsRead.Body(first.Id))));

		var chat = ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(_bob), _chat)));
		Assert.Equal(second.Id, chat.LastReadMessageId);
		Assert.Equal(0, chat.UnreadMessagesCount);
	}

	[Fact]
	public async Task UnreadCountExcludesOwnMessagesAndReadOnes()
	{
		await GivenDirectChat();
		var first = await SendMessage(_alice, _chat, "1");
		await SendMessage(_alice, _chat, "2");
		await SendMessage(_alice, _chat, "3");

		ResultAssert.NoContent(await Host.Send(new MarkChatAsRead(As(_bob), _chat, new MarkChatAsRead.Body(first.Id))));

		Assert.Equal(2, ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(_bob), _chat))).UnreadMessagesCount);
		Assert.Equal(0, ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(_alice), _chat))).UnreadMessagesCount);
	}

	[Fact]
	public async Task MarkingMessageFromAnotherChatAsReadIsRejected()
	{
		await GivenDirectChat();
		var carol = await Host.CreateUser("Carol");
		await MakeFriends(_alice, carol);
		var otherChat = await StartDirectChat(_alice, carol);
		var foreignMessage = await SendMessage(carol, otherChat);

		var result = await Host.Send(new MarkChatAsRead(As(_alice), _chat, new MarkChatAsRead.Body(foreignMessage.Id)));

		ResultAssert.Error(result, "ChatMessage.NotFound", 404);
	}

	[Fact]
	public async Task MessagesCanBePagedBackwards()
	{
		await GivenDirectChat();
		var sent = new List<ChatMessageReadModel>();
		for (var i = 0; i < 5; i++)
			sent.Add(await SendMessage(_alice, _chat, $"m{i}"));

		var page = ResultAssert.Ok<GetChatMessagesResponse>(
			await Host.Send(new GetChatMessages(As(_bob), _chat, null, null, sent[3].Id, null, 2)));

		Assert.Equal([sent[2].Id, sent[1].Id], page.Messages.Select(m => m.Id).OrderByDescending(id => id.Value));
	}

	[Fact]
	public async Task MessageSearchMatchesContentCaseInsensitively()
	{
		await GivenDirectChat();
		await SendMessage(_alice, _chat, "Buy MILK please");
		await SendMessage(_alice, _chat, "something else");

		var page = ResultAssert.Ok<GetChatMessagesResponse>(await Host.Send(new GetChatMessages(As(_bob), _chat, "milk", null, null, null)));

		Assert.Equal(["Buy MILK please"], page.Messages.Select(m => m.Content));
	}
}