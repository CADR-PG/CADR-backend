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

	public void Send()
	{
		if (Status is FriendRequestStatus.Pending) return;
		if (Attempts >= MaxAttempts) return;

		if (Status is FriendRequestStatus.Declined or FriendRequestStatus.Cancelled)
			Attempts++;

		Status = FriendRequestStatus.Pending;
		SentAt = DateTimeOffset.UtcNow;
	}

	public void Cancel()
	{
		Status = FriendRequestStatus.Cancelled;
	}

	public void Decline()
	{
		Status = FriendRequestStatus.Cancelled;
	}

	public static FriendRequest CreateSent(UserId senderId, UserId receiverId) => new()
	{
		Id = Guid.NewGuid(),
		SenderId = senderId,
		ReceiverId = receiverId,
		Status = FriendRequestStatus.Pending,
		Attempts = 1,
		SentAt = DateTimeOffset.UtcNow
	};
}