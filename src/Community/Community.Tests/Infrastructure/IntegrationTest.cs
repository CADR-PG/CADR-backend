using Community.Core.Features.ChatMessages;
using Community.Core.Features.Chats;
using Community.Core.Features.FriendRequests;
using Community.Core.Features.UserBlocks;
using Community.Core.Models.Chats;
using Community.Core.Models.Friendships;
using Community.Core.ValueObjects;
using Shared.Endpoints.Requests;
using Shared.ValueObjects;

namespace Community.Tests.Infrastructure;

[Collection(PostgresCollectionDefinition.Name)]
public abstract class IntegrationTest(PostgresFixture postgres) : IAsyncLifetime
{
	private protected CommunityTestHost Host { get; private set; } = null!;

	public Task InitializeAsync()
	{
		Host = new CommunityTestHost(postgres);
		return Task.CompletedTask;
	}

	public async Task DisposeAsync() => await Host.DisposeAsync();

	private protected static CurrentUser As(UserId userId) => new(userId);

	private protected async Task<Guid> SendFriendRequest(UserId senderId, UserId receiverId)
	{
		var result = await Host.Send(new SendFriendRequest(As(senderId), new SendFriendRequest.Body(receiverId)));
		var readModel = ResultAssert.Ok<OutgoingFriendRequestReadModel>(result);
		Assert.Equal(OutgoingFriendRequestStatus.Sent, readModel.Status);
		return readModel.Id!.Value;
	}

	private protected async Task MakeFriends(UserId firstUserId, UserId secondUserId)
	{
		var requestId = await SendFriendRequest(firstUserId, secondUserId);
		ResultAssert.Ok<FriendReadModel>(await Host.Send(new AcceptFriendRequest(As(secondUserId), requestId)));
	}

	private protected async Task Block(UserId blockerId, UserId blockedId)
		=> ResultAssert.Ok<BlockedUserReadModel>(await Host.Send(new BlockUser(As(blockerId), new BlockUser.Body(blockedId))));

	private protected async Task<ChatId> StartDirectChat(UserId firstUserId, UserId secondUserId)
	{
		var result = await Host.Send(new GetOrStartDirectChat(As(firstUserId), new GetOrStartDirectChat.Body(secondUserId)));
		return ResultAssert.Success<ChatReadModel>(result).Id;
	}

	private protected async Task<ChatId> CreateGroup(UserId ownerId, params UserId[] memberIds)
	{
		var result = await Host.Send(new CreateGroupChat(As(ownerId), new CreateGroupChat.Body("Group", [.. memberIds])));
		return ResultAssert.Created<ChatReadModel>(result).Id;
	}

	private protected async Task<ChatMessageReadModel> SendMessage(UserId senderId, ChatId chatId, string content = "hello", ChatMessageId? replyTo = null)
	{
		var result = await Host.Send(new SendChatMessage(As(senderId), chatId, new SendChatMessage.Body(Guid.NewGuid(), content, replyTo)));
		return ResultAssert.Created<ChatMessageReadModel>(result);
	}
}