// using Microsoft.AspNetCore.Authentication.JwtBearer;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Projects.Core.Database;
using Projects.Core.Features;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Settings;

namespace Projects.Core;

public class ProjectsModule : IModule
{
	public static string Name => "Projects";

	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<ProjectsDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString));
		services.AddScoped<AddProjectHandler>();
		services.AddScoped<DeleteProjectHandler>();
		services.AddScoped<LoadSceneHandler>();
		services.AddScoped<SaveSceneHandler>();
		services.AddScoped<GetAllUserProjectsHandler>();
		services.AddScoped<ModifyProjectHandler>();
		services.AddValidatorsFromAssemblyContaining<ProjectsModule>(includeInternalTypes: true);
	}


	public void MapEndpoints(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGroup(Name.ToLowerInvariant())
			.Map<AddProjectEndpoint>()
			.Map<GetAllUserProjectsEndpoint>()
			.Map<LoadSceneEndpoint>()
			.Map<ModifyProjectEndpoint>()
			.Map<SaveSceneEndpoint>()
			.Map<DeleteProjectEndpoint>();

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<ProjectsDbContext>();
		await dbContext.Database.MigrateAsync();
	}
}