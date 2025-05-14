using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal record UserReadModel(Guid Id, string Email, string FirstName, string LastName, DateTime LastLoggedInAt)
{
	public static UserReadModel From(User user) => new(user.Id, user.Email, user.FirstName, user.LastName, user.LastLoggedInAt);
};