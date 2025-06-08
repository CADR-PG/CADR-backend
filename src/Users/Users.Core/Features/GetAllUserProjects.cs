using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct GetAllUserProjects(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetAllUserProjectsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetAllUserProjects, GetAllUserProjectsHandler>("projects")
		.RequireAuthorization();
}

internal sealed class GetAllUserProjectsHandler(
	UsersDbContext dbContext
	) : IHttpRequestHandler<GetAllUserProjects>
{
	public async Task<IResult> Handle(GetAllUserProjects request, CancellationToken cancellationToken)
	{
		var projects = dbContext.Projects.Where(p => p.UserId == request.CurrentUser.Id);
		var paginatedProjects = await Paginated.Create(projects);
		return Results.Ok(paginatedProjects);
	}
}