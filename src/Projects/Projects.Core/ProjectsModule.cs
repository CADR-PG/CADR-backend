using Shared.Modules;

namespace Projects.Core;

public class ProjectsModule : IModule
{
	public static string Name => "Projects";
	public void Register(IServiceCollection services, IConfiguration configuration)
	{

	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapGroup(Name.ToLowerInvariant());

}