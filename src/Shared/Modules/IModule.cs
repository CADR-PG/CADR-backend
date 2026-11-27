using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.Modules;

public interface IModule
{
	public static abstract string Name { get; }
	/// <summary>Registers module services and endpoints in the API host.</summary>
	public void Register(IHostApplicationBuilder builder);

	/// <summary>Registers module message handlers and their dependencies in the message broker host.</summary>
	public void RegisterMessageBroker(IHostApplicationBuilder builder) { }

	/// <summary>
	/// Registers dependencies of module Azure Functions (defined in the module, e.g. <c>Functions/*Function.cs</c>)
	/// in the Functions worker host.
	/// </summary>
	public void RegisterFunctions(IHostApplicationBuilder builder) { }

	public void MapEndpoints(IEndpointRouteBuilder endpoints);
	public ValueTask RunInDevelopmentMode(IServiceProvider services) => ValueTask.CompletedTask;
}