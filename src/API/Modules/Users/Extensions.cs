using API.Modules.Users.Features;
using API.Shared.Endpoints;

namespace API.Modules.Users;

internal static class Extensions
{
	public static void AddUsersModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<LoginHandler>();
	}

	public static void MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
		=> endpoints.MapGroup("users")
			.Map<LoginEndpoint>();
}
