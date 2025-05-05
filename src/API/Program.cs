using API.Database;
using API.Modules;
using API.Modules.Users;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
	builder.Configuration.AddAzureKeyVault(
		new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
		new DefaultAzureCredential());

	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddHttpContextAccessor();
var connectionString = builder.Configuration.GetConnectionString("Database")
		?? throw new InvalidOperationException("Connection string" + "'Database' not found.");
builder.Services.AddDbContext<CADRDbContext>(options =>
	options.UseNpgsql(connectionString));
builder.Services.AddScoped<CADRDbContext>();

builder.Services.AddAuthentication().AddJwtCookie(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddModules(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference("/docs", options => options
	.WithTitle("CADR API")
	.WithTheme(ScalarTheme.Default)
	.WithDarkMode(true)
	.WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios)
	.WithSidebar(true)
	.WithDownloadButton(true)
	.WithDarkModeToggle(true)
	.WithDotNetFlag(true)
	.WithTestRequestButton(true)
	.WithModels(false)
	.WithDefaultOpenAllTags(false));

app.UseHttpsRedirection();

app.MapEndpoints();

app.UseAuthentication();

await app.RunAsync();