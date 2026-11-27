using Community.Core.Entities.Chats;
using Community.Core.Features.ChatMessages;
using Community.Core.Features.Chats;
using Community.Core.Features.UserBlocks;
using Community.Core.Models.Chats;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Community.Tests.Integration;

public sealed class DirectChatTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task StartingChatCreatesItOnceForBothUsers()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);

		var created = ResultAssert.Created<ChatReadModel>(
			await Host.Send(new GetOrStartDirectChat(As(alice), new GetOrStartDirectChat.Body(bob))));
		var existing = ResultAssert.Ok<ChatReadModel>(
			await Host.Send(new GetOrStartDirectChat(As(bob), new GetOrStartDirectChat.Body(alice))));

		Assert.Equal(created.Id, existing.Id);
		Assert.Equal(ChatType.Direct, created.Type);
		Assert.Equal(bob.Value, created.Counterpart!.UserId);
		Assert.Equal(alice.Value, existing.Counterpart!.UserId);
		Assert.False(created.IsReadOnly);
	}

	[Fact]
	public async Task StartingChatWithSelfIsRejected()
	{
		var alice = await Host.CreateUser("Alice");

		var result = await Host.Send(new GetOrStartDirectChat(As(alice), new GetOrStartDirectChat.Body(alice)));

		ResultAssert.Error(result, "DirectChat.InvalidUser", 400);
	}

	[Fact]
	public async Task StartingChatRequiresFriendship()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");

		var result = await Host.Send(new GetOrStartDirectChat(As(alice), new GetOrStartDirectChat.Body(bob)));

		ResultAssert.Error(result, "DirectChat.NotFriends", 400);
		Assert.False(await Host.Query(db => db.Chats.AnyAsync(chat =>
			chat.Type == ChatType.Direct && (chat.DirectChatLowerUserId == alice || chat.DirectChatHigherUserId == alice))));
	}

	[Fact]
	public async Task OutsiderCannotSeeChat()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		var carol = await Host.CreateUser("Carol");
		await MakeFriends(alice, bob);
		var chatId = await StartDirectChat(alice, bob);

		ResultAssert.Error(await Host.Send(new GetChatById(As(carol), chatId)), "Chat.NotFound", 404);
		ResultAssert.Error(
			await Host.Send(new SendChatMessage(As(carol), chatId, new SendChatMessage.Body(Guid.NewGuid(), "hi", null))),
			"Chat.NotFound", 404);
	}

	[Fact]
	public async Task ChatBecomesReadOnlyAfterBlockAndStaysReadOnlyAfterUnblock()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);
		var chatId = await StartDirectChat(alice, bob);
		var message = await SendMessage(alice, chatId);

		await Block(alice, bob);

		Assert.True(ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(bob), chatId))).IsReadOnly);
		ResultAssert.Error(
			await Host.Send(new SendChatMessage(As(bob), chatId, new SendChatMessage.Body(Guid.NewGuid(), "hi", null))),
			"Chat.ReadOnly", 403);
		ResultAssert.Error(
			await Host.Send(new EditChatMessage(As(alice), chatId, message.Id, new EditChatMessage.Body("edited"))),
			"Chat.ReadOnly", 403);

		ResultAssert.NoContent(await Host.Send(new UnblockUser(As(alice), new UnblockUser.Body(bob))));

		Assert.True(ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(alice), chatId))).IsReadOnly);
	}

	[Fact]
	public async Task RenewedFriendshipMakesChatWritableAgain()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);
		var chatId = await StartDirectChat(alice, bob);
		await Block(alice, bob);
		ResultAssert.NoContent(await Host.Send(new UnblockUser(As(alice), new UnblockUser.Body(bob))));

		await MakeFriends(alice, bob);

		Assert.False(ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(alice), chatId))).IsReadOnly);
		await SendMessage(bob, chatId, "we're back");
	}

	[Fact]
	public async Task ReadingIsAllowedInReadOnlyChat()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);
		var chatId = await StartDirectChat(alice, bob);
		var message = await SendMessage(alice, chatId);
		await Block(bob, alice);

		ResultAssert.NoContent(await Host.Send(new MarkChatAsRead(As(bob), chatId, new MarkChatAsRead.Body(message.Id))));
		var messages = ResultAssert.Ok<GetChatMessagesResponse>(await Host.Send(new GetChatMessages(As(bob), chatId, null, null, null, null)));
		Assert.Contains(messages.Messages, m => m.Id == message.Id);
	}
}