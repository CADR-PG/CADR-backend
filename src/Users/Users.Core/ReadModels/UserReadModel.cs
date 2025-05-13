using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal record UserReadModel(Guid Id, string Email)
{
	public static UserReadModel From(User user) => new(user.Id, user.Email);
};