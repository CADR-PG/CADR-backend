using API.Modules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddModules(configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference(options => options
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
}

app.UseHttpsRedirection();

app.MapEndpoints();

await app.RunAsync();