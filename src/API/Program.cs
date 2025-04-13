using API.Database;
using API.Modules;
using API.Modules.Users.Infrastructure;
using API.Modules.Users.Models;
using API.Modules.Users.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var connectonString = builder.Configuration.GetConnectionString("Database")
		?? throw new InvalidOperationException("Connection string" + "'DefaultConnection' not found.");
builder.Services.AddDbContext<CADRDbContext>(options =>
	options.UseNpgsql(connectonString));
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddScoped<IValidator<User>, UserValidator>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
		ValidateIssuer = true,
		ValidateAudience = true,
	};
});

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

app.UseAuthentication();

await app.RunAsync();