using API.Documentation;
using API.Exceptions;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Shared;
using Shared.Modules;
using Users.Core;


var builder = WebApplication.CreateBuilder(args);

var applicationContext = new ApplicationContext([
	new UsersModule()
]);

if (builder.Environment.IsProduction())
{
	builder.Configuration.AddAzureKeyVault(
		new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
		new DefaultAzureCredential());

	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.RegisterModules(applicationContext);
builder.Services.AddDocumentation();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.Configure<JsonOptions>(options =>
{
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapDocumentation();

app.UseAuthentication();

app.MapEndpoints(applicationContext);

await app.RunAsync();