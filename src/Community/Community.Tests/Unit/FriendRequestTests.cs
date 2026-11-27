using Community.Core.Entities.Friendships;
using Microsoft.Extensions.Time.Testing;
using Shared.ValueObjects;

namespace Community.Tests.Unit;

public sealed class FriendRequestTests
{
	private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

	private FriendRequest NewRequest() => FriendRequest.CreateSent(new UserId(Guid.NewGuid()), new UserId(Guid.NewGuid()), _clock);

	[Fact]
	public void CreateSentIsPendingFirstAttempt()
	{
		var request = NewRequest();

		Assert.Equal(FriendRequestStatus.Pending, request.Status);
		Assert.Equal(1u, request.Attempts);
		Assert.Equal(_clock.GetUtcNow(), request.SentAt);
		Assert.NotEqual(Guid.Empty, request.Id);
	}

	[Fact]
	public void SendWhilePendingDoesNotCountAnotherAttempt()
	{
		var request = NewRequest();
		var sentAt = request.SentAt;
		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.True(request.Send(_clock));
		Assert.Equal(1u, request.Attempts);
		Assert.Equal(sentAt, request.SentAt);
	}

	[Fact]
	public void SendAfterDeclineStartsNextAttempt()
	{
		var request = NewRequest();
		request.Decline();
		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.True(request.Send(_clock));
		Assert.Equal(FriendRequestStatus.Pending, request.Status);
		Assert.Equal(2u, request.Attempts);
		Assert.Equal(_clock.GetUtcNow(), request.SentAt);
	}

	[Fact]
	public void SendIsRejectedAfterMaxAttempts()
	{
		var request = NewRequest();
		for (var attempt = 1; attempt < FriendRequest.MaxAttempts; attempt++)
		{
			request.Decline();
			Assert.True(request.Send(_clock));
		}

		request.Decline();

		Assert.Equal(FriendRequest.MaxAttempts, request.Attempts);
		Assert.False(request.Send(_clock));
		Assert.Equal(FriendRequestStatus.Declined, request.Status);
		Assert.Equal(FriendRequest.MaxAttempts, request.Attempts);
	}

	[Fact]
	public void CancelledRequestCanBeResentAsNextAttempt()
	{
		var request = NewRequest();
		request.Cancel();

		Assert.True(request.Send(_clock));
		Assert.Equal(2u, request.Attempts);
	}

	[Fact]
	public void DeclineDoesNotOverrideCancellation()
	{
		var request = NewRequest();
		request.Cancel();

		request.Decline();

		Assert.Equal(FriendRequestStatus.Cancelled, request.Status);
	}

	[Fact]
	public void CancelDoesNotOverrideDecline()
	{
		var request = NewRequest();
		request.Decline();

		request.Cancel();

		Assert.Equal(FriendRequestStatus.Declined, request.Status);
	}
}