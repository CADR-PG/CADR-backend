using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Community.Core;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Projects.Core;
using Shared;
using Shared.Modules;
using Shop.Core;
using Users.Core;

var builder = FunctionsApplication.CreateBuilder(args);

var applicationContext = new ApplicationContext([
	new UsersModule(),
	new ProjectsModule(),
	new ShopModule(),
	new CommunityModule()
]);

// The worker's working directory is not the app directory, so environment-specific settings are loaded explicitly.
// Environment variables (Function App settings) are re-added afterward to keep their precedence over the file.
builder.Configuration
	.AddJsonFile(
		Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
		optional: true,
		reloadOnChange: false)
	.AddEnvironmentVariables();

if (builder.Environment.IsProduction())
{
	builder.Configuration.AddAzureKeyVault(
		new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
		new DefaultAzureCredential());

	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.RegisterFunctionsModules(applicationContext);

using var host = builder.Build();
await host.RunAsync();