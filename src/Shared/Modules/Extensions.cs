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

	public static void RegisterMessageBrokerModules(this IHostApplicationBuilder builder, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.RegisterMessageBroker(builder);
	}

	public static void RegisterFunctionsModules(this IHostApplicationBuilder builder, ApplicationContext applicationContext)
	{
		foreach (var module in applicationContext.Modules)
			module.RegisterFunctions(builder);
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

		var handlerTypes = typeof(TModule).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
			.Select(t => (Type: t, Interfaces: t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerInterfaceType)
				.ToList()))
			.Where(x => x.Interfaces.Count > 0);

		foreach (var (handlerType, interfaces) in handlerTypes)
		{
			// endpoints (Map*<TRequest, THandler>) resolve the concrete handler type
			services.TryAdd(new ServiceDescriptor(handlerType, handlerType, lifetime));

			foreach (var handlerInterface in interfaces)
				services.TryAddEnumerable(new ServiceDescriptor(handlerInterface, handlerType, lifetime));
		}
	}

	public static void MapEndpoints<TModule>(this IEndpointRouteBuilder endpoints) where TModule : IModule
		=> endpoints.MapEndpoints<TModule>(_ => true);

	public static void MapEndpoints<TModule>(this IEndpointRouteBuilder endpoints, Func<Type, bool> predicate) where TModule : IModule
	{
		var invoker = typeof(Endpoints.Extensions)
			.GetMethod(nameof(Endpoints.Extensions.Map), BindingFlags.Public | BindingFlags.Static)!;

		var types = typeof(TModule).Assembly.GetTypes()
			.Where(t => t is { IsAbstract: false, IsInterface: false }
						&& t.IsAssignableTo(typeof(IEndpoint))
						&& predicate(t));

		foreach (var type in types)
			invoker.MakeGenericMethod(type).Invoke(null, [endpoints]);
	}
}