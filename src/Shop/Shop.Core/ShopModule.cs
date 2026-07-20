using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Settings;
using Shop.Core.Database;
using System.Runtime.CompilerServices;

namespace Shop.Core;

public class ShopModule : IModule
{
	public static string Name => "Shop";

	public void Register(IHostApplicationBuilder builder)
	{
		var services = builder.Services;
		var configuration = builder.Configuration;

		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<ShopDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString,
			x => x.MigrationsHistoryTable("__EFMigrationsHistory", Name)));
		services.AddAzureClients(builder =>
		{
			var projectSettings = configuration.GetSection("Azure");
			var connectionString = projectSettings["StorageAccountConnectionString"];

		});
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{

	}

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<ShopDbContext>();
		await dbContext.Database.MigrateAsync();
	}
}