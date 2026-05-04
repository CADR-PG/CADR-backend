using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.Modules;

public interface IModule
{
	public static abstract string Name { get; }
	public void Register(IHostApplicationBuilder builder);
	public void MapEndpoints(IEndpointRouteBuilder endpoints);
	public ValueTask RunInDevelopmentMode(IServiceProvider services) => ValueTask.CompletedTask;
}