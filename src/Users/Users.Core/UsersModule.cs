using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Services;
using Shared.Settings;
using System.Runtime.CompilerServices;
using Users.Core.Database;
using Users.Core.Features;
using Users.Core.Services;
using Users.Core.Settings;

[assembly: InternalsVisibleTo("Users.Tests")]
namespace Users.Core;

public class UsersModule : IModule
{
	public static string Name => "Users";

	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString));
		services.AddSettingsWithOptions<JwtSettings>(configuration);
		services.AddMailingService(configuration);
		services.AddScoped<LoginHandler>();
		services.AddScoped<RegisterHandler>();
		services.AddScoped<LogoutHandler>();
		services.AddScoped<RefreshHandler>();
		services.AddScoped<GetCurrentUserHandler>();
		services.AddScoped<AddProjectHandler>();
		services.AddScoped<GetAllUserProjectsHandler>();
		services.AddScoped<ModifyProjectHandler>();
		services.AddScoped<SaveSceneHandler>();
		services.AddScoped<LoadSceneHandler>();
		services.AddScoped<ChangeUserInfoHandler>();
		services.AddScoped<ChangeEmailHandler>();
		services.AddScoped<ChangePasswordHandler>();
		services.AddScoped<ConfirmEmailHandler>();
		services.AddScoped<ResendEmailConfirmationHandler>();
		services.AddScoped<GetCurrentUserHandler>();
		services.AddSingleton<ITokenProvider, JwtTokenProvider>();
		services.AddScoped<EmailConfirmationService>();
		services.AddValidatorsFromAssemblyContaining<UsersModule>(includeInternalTypes: true);

		var jwtSettings = configuration.GetSettings<JwtSettings>();

		services.AddAuthentication().AddJwtBearer(options =>
		{
			options.MapInboundClaims = false;
			options.TokenValidationParameters = JwtTokenProvider.GetAccessTokenValidationParameters(jwtSettings);
			options.Events = new JwtBearerEvents
			{
				OnMessageReceived = context =>
				{
					context.Token = context.Request.Cookies[CookieTokenStorage.AccessTokenCookieKey];
					return Task.CompletedTask;
				},
				OnChallenge = context =>
				{
					context.HandleResponse();
					return SharedErrors.UnauthorizedError.ExecuteAsync(context.HttpContext);
				},
			};
		});
		services.AddAuthorization();
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGroup(Name.ToLowerInvariant())
			.Map<LoginEndpoint>()
			.Map<RegisterEndpoint>()
			.Map<RefreshEndpoint>()
			.Map<LogoutEndpoint>()
			.Map<GetCurrentUserEndpoint>()
			.Map<AddProjectEndpoint>()
			.Map<GetAllUserProjectsEndpoint>()
			.Map<ModifyProjectEndpoint>()
			.Map<SaveSceneEndpoint>()
			.Map<LoadSceneEndpoint>()
			.Map<ChangeUserInfoEndpoint>()
			.Map<ChangeEmailEndpoint>()
			.Map<ConfirmEmailEndpoint>()
			.Map<ChangePasswordEndpoint>()
			.Map<ResendEmailConfirmationEndpoint>();
}