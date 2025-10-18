using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Shared.Modules;

public static class Extensions
{
	public static void RegisterModules(this IHostApplicationBuilder builder, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.Register(builder.Services, builder.Configuration);
	}

	public static void MapEndpoints(this IEndpointRouteBuilder endpoints, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.MapEndpoints(endpoints);
	}

	public static void ApplyMigration(this IEndpointRouteBuilder endpoints, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.MapEndpoints(endpoints);
	}
}