using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.ReadModels;
using Users.Core.Database;
using Users.Core.ReadModels;

namespace Users.Core.Features.LoginLocations;

internal record struct GetAllCurrentUserSafeAccessPoints(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetAllCurrentUserSafeAccessPointsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GetCurrentUserSafeAccessPoint, GetAllCurrentUserSafeAccessPointsHandler>("safe-access-points")
			.Produces<ItemList<UserSafeAccessPointReadModel>>()
			.RequireAuthorization()
			.ProducesError(400, $"`{nameof(Errors.InvalidRefreshCredentialsError)}`")
			.WithDescription($"Returns all user safe access points (max. 10).");
}

internal sealed class GetAllCurrentUserSafeAccessPointsHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<GetCurrentUserSafeAccessPoint>
{
	public async Task<IResult> Handle(GetCurrentUserSafeAccessPoint request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;

		var safeAccessPoints = await dbContext.UserSafeAccessPoints
			.Where(x => x.UserId == userId)
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync(cancellationToken);

		return Results.Ok(ItemList.From(safeAccessPoints));
	}
}