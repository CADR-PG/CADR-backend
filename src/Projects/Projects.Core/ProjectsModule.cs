using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Projects.Core.Database;
using Projects.Core.Entities;
using Projects.Core.Features;
using Projects.Core.Features.Assets;
using Projects.Core.Features.Projects;
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
		services.AddDbContext<ProjectsDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", Name)));
		services.AddScoped<AddProjectHandler>();
		services.AddScoped<DeleteProjectHandler>();
		services.AddScoped<LoadSceneHandler>();
		services.AddScoped<SaveSceneHandler>();
		services.AddScoped<GetAllUserProjectsHandler>();
		services.AddScoped<ModifyProjectHandler>();
		services.AddScoped<CreateAssetHandler>();
		services.AddScoped<DeleteAssetHandler>();
		services.AddScoped<MoveAssetHandler>();
		services.AddScoped<AssetsTreeHandler>();
		services.AddValidatorsFromAssemblyContaining<ProjectsModule>(includeInternalTypes: true);
		services.AddAzureClients(builder =>
		{
			var projectSettings = configuration.GetSection("Azure");
			var connectionString = projectSettings["StorageAccountConnectionString"];

			builder.AddBlobServiceClient(connectionString);
		});
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGroup(Name.ToLowerInvariant())
			.WithTags(Name)
			.Map<AddProjectEndpoint>()
			.Map<GetAllUserProjectsEndpoint>()
			.Map<LoadSceneEndpoint>()
			.Map<ModifyProjectEndpoint>()
			.Map<SaveSceneEndpoint>()
			.Map<DeleteProjectEndpoint>()
			.Map<CreateAssetEndpoint>()
			.Map<DeleteAssetEndpoint>()
			.Map<MoveAssetEndpoint>()
			.Map<AssetsTreeEndpoint>();

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<ProjectsDbContext>();
		await dbContext.Database.MigrateAsync();

		var blobServiceClient = services.GetRequiredService<BlobServiceClient>();
		var containerClient = blobServiceClient.GetBlobContainerClient(Asset.BlobContainerName);

		bool exists = await containerClient.ExistsAsync();

		if (!exists)
			await containerClient.CreateAsync();
	}
}