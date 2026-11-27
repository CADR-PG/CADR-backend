using Shared.ValueObjects;
using Users.Contracts.ReadModels;

namespace Users.Contracts;

/// <summary>
/// Public API of the Users module for other modules.
/// </summary>
public interface IUsersProvider
{
	/// <returns>The user, or <c>null</c> when the user does not exist.</returns>
	public Task<UserReadModel?> GetUser(UserId userId, CancellationToken cancellationToken);

	/// <summary>
	/// Page of user ids ordered by id (keyset pagination) - lets other modules find users missing in their data
	/// without loading the users; details of the missing ones are then fetched with <see cref="GetUsers" />.
	/// </summary>
	/// <param name="after">The last id of the previous page, <c>null</c> for the first page.</param>
	/// <param name="limit">Maximum number of returned ids.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Ids greater than <paramref name="after" />; fewer than <paramref name="limit" /> on the last page.</returns>
	public Task<IReadOnlyList<UserId>> GetUserIds(UserId? after, int limit, CancellationToken cancellationToken);

	/// <summary>Users with the given ids - ids of non-existing users are skipped.</summary>
	public Task<IReadOnlyList<UserReadModel>> GetUsers(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken);
}