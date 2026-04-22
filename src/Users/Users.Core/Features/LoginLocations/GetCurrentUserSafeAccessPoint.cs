using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;

namespace Users.Core.Features.LoginLocations;

internal record struct GetCurrentUserSafeAccessPoint(CurrentUser CurrentUser, [FromRoute] Guid PointId) : IHttpRequest;

internal sealed class GetCurrentUserSafeAccessPointEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GetCurrentUserSafeAccessPoint, GetCurrentUserSafeAccessPointHandler>("safe-access-points/{pointId}")
			.Produces<UserSafeAccessPointReadModel>()
			.RequireAuthorization()
			.ProducesError(404, $"`SafeAccessPointNotFound`")
			.WithDescription($"Returns user safe access point for given ID");
}

internal sealed class GetCurrentUserSafeAccessPointHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<GetCurrentUserSafeAccessPoint>
{
	public async Task<IResult> Handle(GetCurrentUserSafeAccessPoint request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;

		var safeAccessPoints = await dbContext.UserSafeAccessPoints
			.Where(x => x.UserId == userId && x.Id == request.PointId)
			.FirstOrDefaultAsync(cancellationToken);

		return safeAccessPoints is null
			? new ErrorResult("SafeAccessPointNotFound", $"Safe access point with ID `{safeAccessPoints}` not found.", 404)
			: Results.Ok(UserSafeAccessPointReadModel.From(safeAccessPoints));
	}
}