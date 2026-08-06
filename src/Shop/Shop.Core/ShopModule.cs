using Azure.Identity;
using Azure.Storage.Blobs;
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
using Shop.Core.Entities.Catalog;
using Shop.Core.Features;
using Shop.Core.Services;
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
		services.AddScoped<AddGameToStoreHandler>();
		services.AddScoped<CreateSnapshotHandler>();
		services.AddScoped<PublishVersionHandler>();
		services.AddScoped<GameSnapshotService>();
		services.AddScoped<GetListOfGamesHandler>();
		services.AddScoped<BuyGameHandler>();
		services.AddScoped<LibraryService>();
		services.AddSingleton<FilesContainerClient>();
		services.AddAzureClients(builder =>
		{
			var projectSettings = configuration.GetSection("Azure");
			var connectionString = projectSettings["StorageAccountConnectionString"];
			builder.AddBlobServiceClient(connectionString)
				.WithVersion(BlobClientOptions.ServiceVersion.V2025_07_05);
		});
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		var shop = endpoints.MapGroup(Name.ToLowerInvariant()).WithTags(Name);
		shop.Map<AddGameToStoreEndpoint>();
		shop.Map<CreateSnapshotEndpoint>();
		shop.Map<PublishVersionEndpoint>();
		shop.Map<GetListOfGamesEndpoint>();
		shop.Map<BuyGameEndpoint>();
	}

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<ShopDbContext>();
		await dbContext.Database.MigrateAsync();

		var blobServiceClient = services.GetRequiredService<BlobServiceClient>();
		var containerClient = blobServiceClient.GetBlobContainerClient(GameVersion.BlobContainerName);

		bool exists = await containerClient.ExistsAsync();

		if (!exists)
			await containerClient.CreateAsync();
	}
}