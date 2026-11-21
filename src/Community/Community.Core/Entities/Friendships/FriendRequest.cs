using Community.Core.Entities.UserProfiles;
using Shared.ValueObjects;

namespace Community.Core.Entities.Friendships;

internal enum FriendRequestStatus
{
	Pending = 0,
	Declined = 10,
	Cancelled = 100
}

internal sealed class FriendRequest
{
	public const uint MaxAttempts = 3;

	public required Guid Id { get; init; }

	public required UserId SenderId { get; init; }
	public UserProfile? SenderProfile { get; init; }

	public required UserId ReceiverId { get; init; }
	public UserProfile? ReceiverProfile { get; init; }

	public FriendRequestStatus Status { get; private set; }

	public uint Attempts { get; private set; }
	public DateTimeOffset SentAt { get; private set; }

	private FriendRequest() { }

	public bool Send(TimeProvider timeProvider)
	{
		if (Status is FriendRequestStatus.Pending) return true;
		if (Attempts >= MaxAttempts) return false;

		Attempts++;
		Status = FriendRequestStatus.Pending;
		SentAt = timeProvider.GetUtcNow();
		return true;
	}

	public void Cancel()
	{
		if (Status is FriendRequestStatus.Pending)
			Status = FriendRequestStatus.Cancelled;
	}

	public void Decline()
	{
		if (Status is FriendRequestStatus.Pending)
			Status = FriendRequestStatus.Declined;
	}

	public static FriendRequest CreateSent(UserId senderId, UserId receiverId, TimeProvider timeProvider) => new()
	{
		Id = Guid.NewGuid(),
		SenderId = senderId,
		ReceiverId = receiverId,
		Status = FriendRequestStatus.Pending,
		Attempts = 1,
		SentAt = timeProvider.GetUtcNow()
	};
}