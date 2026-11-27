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
}