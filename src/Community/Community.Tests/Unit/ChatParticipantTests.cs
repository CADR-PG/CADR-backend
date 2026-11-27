using Community.Core.Entities.Chats;
using Community.Core.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.ValueObjects;

namespace Community.Tests.Unit;

public sealed class ChatParticipantTests
{
	private static readonly ChatId Chat = new(1);
	private static readonly DateTimeOffset JoinedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

	private readonly FakeTimeProvider _clock = new(JoinedAt.AddDays(1));

	private static ChatParticipant NewAdmin() => ChatParticipant.CreateAdmin(Chat, new UserId(Guid.NewGuid()), JoinedAt);
	private static ChatParticipant NewMember() => ChatParticipant.CreateParticipant(Chat, new UserId(Guid.NewGuid()), JoinedAt);

	[Fact]
	public void MarkAsReadSetsFirstReadMarker()
	{
		var participant = NewMember();

		Assert.True(participant.MarkAsRead(new ChatMessageId(10), _clock));
		Assert.Equal(new ChatMessageId(10), participant.LastReadMessageId);
		Assert.Equal(_clock.GetUtcNow(), participant.LastMessageReadAt);
	}

	[Theory]
	[InlineData(9)]
	[InlineData(10)]
	public void MarkAsReadIgnoresOlderOrSameMessage(long messageId)
	{
		var participant = NewMember();
		participant.MarkAsRead(new ChatMessageId(10), _clock);
		var readAt = participant.LastMessageReadAt;
		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.False(participant.MarkAsRead(new ChatMessageId(messageId), _clock));
		Assert.Equal(new ChatMessageId(10), participant.LastReadMessageId);
		Assert.Equal(readAt, participant.LastMessageReadAt);
	}

	[Fact]
	public void MarkAsReadMovesMarkerForward()
	{
		var participant = NewMember();
		participant.MarkAsRead(new ChatMessageId(10), _clock);

		Assert.True(participant.MarkAsRead(new ChatMessageId(11), _clock));
		Assert.Equal(new ChatMessageId(11), participant.LastReadMessageId);
	}

	[Fact]
	public void DeactivateDemotesAdmin()
	{
		var participant = NewAdmin();

		participant.Deactivate(_clock);

		Assert.Equal(_clock.GetUtcNow(), participant.DeactivatedAt);
		Assert.Equal(ChatParticipantRole.Member, participant.Role);
	}

	[Fact]
	public void DeactivateKeepsFirstDeactivationTime()
	{
		var participant = NewMember();
		participant.Deactivate(_clock);
		var firstDeactivation = participant.DeactivatedAt;
		_clock.Advance(TimeSpan.FromHours(1));

		participant.Deactivate(_clock);

		Assert.Equal(firstDeactivation, participant.DeactivatedAt);
	}

	[Fact]
	public void ReactivateClearsDeactivationAndSetsNewJoinDate()
	{
		var participant = NewAdmin();
		participant.Deactivate(_clock);
		var rejoinedAt = JoinedAt.AddDays(5);

		participant.Reactivate(rejoinedAt);

		Assert.Null(participant.DeactivatedAt);
		Assert.Equal(rejoinedAt, participant.JoinedAt);
		Assert.Equal(ChatParticipantRole.Member, participant.Role);
	}

	[Fact]
	public void ReactivateIsIgnoredForActiveParticipant()
	{
		var participant = NewAdmin();

		participant.Reactivate(JoinedAt.AddDays(5));

		Assert.Equal(JoinedAt, participant.JoinedAt);
		Assert.Equal(ChatParticipantRole.Admin, participant.Role);
	}

	[Fact]
	public void PromoteAndDemoteChangeRole()
	{
		var participant = NewMember();

		participant.PromoteToAdmin();
		Assert.Equal(ChatParticipantRole.Admin, participant.Role);

		participant.DemoteToMember();
		Assert.Equal(ChatParticipantRole.Member, participant.Role);
	}
}