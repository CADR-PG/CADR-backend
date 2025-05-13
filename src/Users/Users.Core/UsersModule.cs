using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Settings;
using Users.Core.Database;
using Users.Core.Features;
using Users.Core.Services;
using Users.Core.Settings;

namespace Users.Core;

public class UsersModule : IModule
{
	public static string Name => "Users";

	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString));
		services.AddSettingsWithOptions<JwtSettings>();
		services.AddScoped<LoginHandler>();
		services.AddScoped<RegisterHandler>();
		services.AddScoped<LogoutHandler>();
		services.AddScoped<RefreshHandler>();
		services.AddSingleton<ITokenProvider, JwtTokenProvider>();

		var jwtSettings = configuration.GetSettings<JwtSettings>();

		services.AddAuthentication().AddJwtBearer(options =>
		{
			options.TokenValidationParameters = JwtTokenProvider.GetAccessTokenValidationParameters(jwtSettings);
			options.Events = new JwtBearerEvents
			{
				OnMessageReceived = context =>
				{
					context.Token = context.Request.Query[CookieTokenStorage.AccessTokenCookieKey];
					return Task.CompletedTask;
				}
			};
		});
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGroup(Name)
			.Map<LoginEndpoint>()
			.Map<RegisterEndpoint>()
			.Map<RefreshEndpoint>()
			.Map<LogoutEndpoint>();
}