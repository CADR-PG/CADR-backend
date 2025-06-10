using API.Documentation;
using API.Exceptions;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
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

const string CorsPolicyName = "CADR";
builder.Services.AddCors(options =>
{
	options.AddPolicy(name: CorsPolicyName,
		policy =>
		{
			policy.WithOrigins("https://localhost:5173/", "https://cadr.studio/")
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials();
		});
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapDocumentation();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints(applicationContext);

await app.RunAsync();