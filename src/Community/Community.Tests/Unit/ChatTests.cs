using Community.Core.Entities.Chats;
using Community.Tests.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using Shared.ValueObjects;

namespace Community.Tests.Unit;

public sealed class ChatTests
{
	private static readonly UserId LowerUser = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
	private static readonly UserId HigherUser = new(Guid.Parse("ffffffff-0000-0000-0000-000000000000"));

	private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
	private readonly SequentialSnowflakeIdProvider _ids = new();

	[Fact]
	public void CreateDirectChatOrdersUsersRegardlessOfArgumentOrder()
	{
		var chat = Chat.CreateDirectChat(HigherUser, LowerUser, _ids, _clock);

		Assert.Equal(ChatType.Direct, chat.Type);
		Assert.Null(chat.Name);
		Assert.Equal(LowerUser, chat.DirectChatLowerUserId);
		Assert.Equal(HigherUser, chat.DirectChatHigherUserId);
		Assert.Equal(_clock.GetUtcNow(), chat.CreatedAt);
	}

	[Fact]
	public void CreateDirectChatAddsBothUsersAsMembers()
	{
		var chat = Chat.CreateDirectChat(LowerUser, HigherUser, _ids, _clock);

		Assert.Equal([LowerUser, HigherUser], chat.Participants.Select(p => p.UserId).OrderBy(id => id.Value).ToArray());
		Assert.All(chat.Participants, participant =>
		{
			Assert.Equal(ChatParticipantRole.Member, participant.Role);
			Assert.Equal(chat.Id, participant.ChatId);
			Assert.Equal(chat.CreatedAt, participant.JoinedAt);
		});
	}

	[Fact]
	public void CreateGroupChatTrimsNameAndMakesOwnerTheOnlyAdmin()
	{
		var chat = Chat.CreateGroupChat("  Team  ", LowerUser, [HigherUser], _ids, _clock);

		Assert.Equal(ChatType.Group, chat.Type);
		Assert.Equal("Team", chat.Name);
		Assert.Null(chat.DirectChatLowerUserId);
		Assert.Null(chat.DirectChatHigherUserId);
		var admin = Assert.Single(chat.Participants, p => p.Role == ChatParticipantRole.Admin);
		Assert.Equal(LowerUser, admin.UserId);
		Assert.Equal(HigherUser, Assert.Single(chat.Participants, p => p.Role == ChatParticipantRole.Member).UserId);
	}

	[Fact]
	public void CreateGroupChatDoesNotAddOwnerTwiceWhenListedAsMember()
	{
		var chat = Chat.CreateGroupChat("Team", LowerUser, [LowerUser, HigherUser], _ids, _clock);

		Assert.Equal(2, chat.Participants.Count);
		Assert.Single(chat.Participants, p => p.UserId == LowerUser);
	}

	[Fact]
	public void ChangeNameTrimsNameOfGroupChat()
	{
		var chat = Chat.CreateGroupChat("Team", LowerUser, [], _ids, _clock);

		chat.ChangeName("  New name ");

		Assert.Equal("New name", chat.Name);
	}

	[Fact]
	public void ChangeNameIsIgnoredForDirectChat()
	{
		var chat = Chat.CreateDirectChat(LowerUser, HigherUser, _ids, _clock);

		chat.ChangeName("Renamed");

		Assert.Null(chat.Name);
	}

	[Fact]
	public void MarkAsDeletedKeepsFirstDeletion()
	{
		var chat = Chat.CreateGroupChat("Team", LowerUser, [HigherUser], _ids, _clock);
		var firstDeletionAt = _clock.GetUtcNow();

		chat.MarkAsDeleted(LowerUser, _clock);
		_clock.Advance(TimeSpan.FromHours(1));
		chat.MarkAsDeleted(HigherUser, _clock);

		Assert.Equal(firstDeletionAt, chat.DeletedAt);
		Assert.Equal(LowerUser, chat.DeletedBy);
	}

	[Fact]
	public void MarkAsDeletedIsIgnoredForDirectChat()
	{
		var chat = Chat.CreateDirectChat(LowerUser, HigherUser, _ids, _clock);

		chat.MarkAsDeleted(LowerUser, _clock);

		Assert.Null(chat.DeletedAt);
		Assert.Null(chat.DeletedBy);
	}
}