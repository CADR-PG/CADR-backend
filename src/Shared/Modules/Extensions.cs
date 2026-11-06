using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shared.Endpoints;
using System.Reflection;

namespace Shared.Modules;

public static class Extensions
{
	public static void RegisterModules(this IHostApplicationBuilder builder, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.Register(builder);
	}

	public static void MapEndpoints(this IEndpointRouteBuilder endpoints, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.MapEndpoints(endpoints);
	}

	public static void RegisterHttpRequestHandlers<TModule>(
		this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Scoped) where TModule : IModule
	{
		var openHandlerInterfaceType = typeof(IHttpRequestHandler<>);

		var serviceDescriptors = typeof(TModule).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
			.SelectMany(t => t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerInterfaceType)
				.Select(i => new ServiceDescriptor(i, t, lifetime)));

		foreach (var descriptor in serviceDescriptors) services.TryAddEnumerable(descriptor);
	}

	public static void MapEndpoints<TModule>(this IEndpointRouteBuilder endpoints) where TModule : IModule
	{
		var invoker = typeof(Endpoints.Extensions)
			.GetMethod(nameof(Endpoints.Extensions.Map), BindingFlags.Public | BindingFlags.Static)!;

		var types = typeof(TModule).Assembly.GetTypes()
			.Where(t => t is { IsAbstract: false, IsInterface: false }
			            && t.IsAssignableTo(typeof(IEndpoint)));

		foreach (var type in types)
			invoker.MakeGenericMethod(type).Invoke(null, [endpoints]);
	}
}