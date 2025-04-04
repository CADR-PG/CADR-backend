using API.Modules.Users;

namespace API.Modules;

internal static class Extensions
{
	public static void AddModules(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddUsersModule(configuration);
	}

	public static void MapEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapUsersEndpoints();
	}
}