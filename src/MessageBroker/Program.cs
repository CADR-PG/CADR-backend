using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Community.Core;
using Projects.Core;
using Shared;
using Shared.Messaging;
using Shared.Modules;
using Shop.Core;
using Users.Core;

var builder = Host.CreateApplicationBuilder(args);

var applicationContext = new ApplicationContext([
	new UsersModule(),
	new ProjectsModule(),
	new ShopModule(),
	new CommunityModule()
]);

if (builder.Environment.IsProduction())
{
	builder.Configuration.AddAzureKeyVault(
		new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
		new DefaultAzureCredential());

	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.AddMessaging("message-broker", handlesMessages: true);
builder.RegisterMessageBrokerModules(applicationContext);

using var host = builder.Build();
await host.RunAsync();