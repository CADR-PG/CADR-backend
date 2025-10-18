using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Modules;

public interface IModule
{
	public static abstract string Name { get; }
	public void Register(IServiceCollection services, IConfiguration configuration);
	public void MapEndpoints(IEndpointRouteBuilder endpoints);
	public ValueTask RunInDevelopmentMode(IServiceProvider services) => ValueTask.CompletedTask;
}