using API.Modules.Users.Entities;

namespace API.Modules.Users.ReadModels;

internal record UserReadModel(Guid Id, string Email)
{
	public static UserReadModel From(User user) => new(user.Id, user.Email);
};