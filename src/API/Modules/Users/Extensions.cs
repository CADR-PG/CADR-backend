using API.Modules.Users.Features;
using API.Modules.Users.Services;
using API.Modules.Users.Settings;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API.Modules.Users;

internal static class Extensions
{
	public static void AddUsersModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<LoginHandler>();
		services.AddScoped<RegisterHandler>();
		services.AddScoped<LogoutHandler>();
		services.AddScoped<RefreshHandler>();
		services.AddSingleton<IUserTokensProvider, JwtProvider>();
		services.AddSingleton<UserTokensHttpStorage>();
		services.Configure<UserTokensSettings>(configuration.GetSection(UserTokensSettings.SectionName));

	}

	public static void MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGroup("users")
			.Map<LoginEndpoint>()
			.Map<RegisterEndpoint>()
			.Map<RefreshEndpoint>()
			.Map<LogoutEndpoint>();
	}

	public static void AddJwtCookie(this AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
	{
		var userTokensSettings = configuration.GetSection(UserTokensSettings.SectionName).Get<UserTokensSettings>()!;

		authenticationBuilder.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(userTokensSettings.AccessSecret)),
				ValidateIssuer = true,
				ValidIssuer = userTokensSettings.Issuer,
				ValidateAudience = true,
				ValidAudience = userTokensSettings.Audience,
			};
			options.Events = new JwtBearerEvents
			{
				OnMessageReceived = context =>
				{
					context.Token = context.Request.Query[UserTokensHttpStorage.AccessTokenCookieKey];
					return Task.CompletedTask;
				}
			};
		});
	}
}