using Shared.ValueObjects;
using System.Collections.Concurrent;
using Users.Contracts;
using Users.Contracts.ReadModels;

namespace Community.Tests.Infrastructure;

/// <summary>Users known to the (faked) Users module.</summary>
internal sealed class FakeUsers
{
	private readonly ConcurrentDictionary<UserId, UserReadModel> _users = new();

	public void Add(UserId userId, string email)
		=> _users[userId] = new UserReadModel(userId, email, IsEmailConfirmed: false, DateTime.UtcNow);

	public UserReadModel? Find(UserId userId) => _users.GetValueOrDefault(userId);
}

/// <summary>Internal and scoped - registered the same way as the real Users module implementation.</summary>
internal sealed class FakeUsersProvider(FakeUsers users) : IUsersProvider
{
	public Task<UserReadModel?> GetUser(UserId userId, CancellationToken cancellationToken)
		=> Task.FromResult(users.Find(userId));
}