using API.Modules.Users.Features;
using API.Shared.Endpoints;

namespace API.Modules.Users;

internal static class Extensions
{
	public static void AddUsersModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<LoginHandler>();
		services.AddScoped<RegisterHandler>();
		services.AddScoped<LogoutHandler>();
		services.AddScoped<RefreshHandler>();
		services.AddScoped<LogoutHandler>();
	}

	public static void MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGroup("users")
			.Map<LoginEndpoint>();
		endpoints.MapGroup("users")
			.Map<RegisterEndpoint>();
		endpoints.MapGroup("users")
			.Map<LogoutEndpoint>();
		endpoints.MapGroup("users")
			.Map<RefreshEndpoint>();
	}
}