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

	/// <summary>Ids of users whose details were requested with <see cref="IUsersProvider.GetUsers" />.</summary>
	public ConcurrentBag<UserId> RequestedUserIds { get; } = [];

	/// <summary>Same ordering as the real implementation (PostgreSQL <c>uuid</c> order equals <see cref="Guid" /> order).</summary>
	public IReadOnlyList<UserId> PageIds(UserId? after, int limit)
		=> [.. _users.Keys
			.Where(userId => after is null || userId > after.Value)
			.OrderBy(userId => userId.Value)
			.Take(limit)];

	public IReadOnlyList<UserReadModel> FindAll(IReadOnlyCollection<UserId> userIds)
	{
		foreach (var userId in userIds)
			RequestedUserIds.Add(userId);

		return [.. userIds.Select(Find).OfType<UserReadModel>()];
	}
}

/// <summary>Internal and scoped - registered the same way as the real Users module implementation.</summary>
internal sealed class FakeUsersProvider(FakeUsers users) : IUsersProvider
{
	public Task<UserReadModel?> GetUser(UserId userId, CancellationToken cancellationToken)
		=> Task.FromResult(users.Find(userId));

	public Task<IReadOnlyList<UserId>> GetUserIds(UserId? after, int limit, CancellationToken cancellationToken)
		=> Task.FromResult(users.PageIds(after, limit));

	public Task<IReadOnlyList<UserReadModel>> GetUsers(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken)
		=> Task.FromResult(users.FindAll(userIds));
}