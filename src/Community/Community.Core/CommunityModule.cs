using Community.Core.Database;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Modules;
using Shared.Settings;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Community.Tests")]
namespace Community.Core;

public class CommunityModule : IModule
{
	public static string Name => "Community";

	public void Register(IHostApplicationBuilder builder)
	{
		var services = builder.Services;
		var configuration = builder.Configuration;

		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString, x => x.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Name)));
		services.RegisterHttpRequestHandlers<CommunityModule>();
		services.AddValidatorsFromAssemblyContaining<CommunityModule>(includeInternalTypes: true);
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapEndpoints<CommunityModule>();

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<CommunityDbContext>();
		await dbContext.Database.MigrateAsync();
	}
}