using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Users.Core.Database;
using Users.Core.ReadModels;

namespace Users.Core.Features;

internal record struct GetCurrentUser(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetCurrentUserEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GetCurrentUser, GetCurrentUserHandler>("me")
			.RequireAuthorization()
			.Produces<UserReadModel>()
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Get current logged in user. Returns `{nameof(UserReadModel)}`.");
}

internal sealed class GetCurrentUserHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<GetCurrentUser>
{
	public async Task<IResult> Handle(GetCurrentUser request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.FirstAsync(x => x.Id == request.CurrentUser.Id, cancellationToken);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}