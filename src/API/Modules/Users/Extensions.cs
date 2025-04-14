using API.Modules.Users.Features;
using API.Shared.Endpoints;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Modules.Users.Services;
using API.Modules.Users.Validators;
using FluentValidation;

namespace API.Modules.Users;

internal static class Extensions
{
	public static void AddUsersModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<LoginHandler>();
		services.AddScoped<RegisterHandler>();
		services.AddScoped<LogoutHandler>();
		services.AddScoped<RefreshHandler>();
		services.AddSingleton<TokenProvider>();
		services.AddSingleton<OptionsInjector>();
		services.AddScoped<IValidator<User>, UserValidator>();
		services.AddScoped<UserTokenAuthenticator>();
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
