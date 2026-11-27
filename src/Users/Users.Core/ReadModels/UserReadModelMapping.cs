using Users.Contracts.ReadModels;
using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal static class UserReadModelMapping
{
	public static UserReadModel ToReadModel(this User user)
		=> new(user.Id, user.Email, user.EmailConfirmation.IsConfirmed, user.LastLoggedInAt);
}