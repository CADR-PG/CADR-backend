using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Users.Core.Database;
using Users.Core.ReadModels;

namespace Users.Core.Features.LoginLocations;

internal record struct ChangeCurrentUserSafeAccessPoint(CurrentUser CurrentUser, [FromRoute] Guid PointId, [FromBody] ChangeCurrentUserSafeAccessPoint.Data Body) : IHttpRequest
{
	public sealed record Data
	{
		public required string Name { get; set; }

		public required double Latitude { get; set; }
		public required double Longitude { get; set; }

		public required double Radius { get; set; }
	};
}

internal sealed class ChangeCurrentUserSafeAccessPointEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPut<ChangeCurrentUserSafeAccessPoint, ChangeCurrentUserSafeAccessPointHandler>("safe-access-points/{pointId}")
			.Produces<UserSafeAccessPointReadModel>()
			.RequireAuthorization()
			.WithDescription("Changes and returns the user safe access point for given ID.");
}

internal sealed class ChangeCurrentUserSafeAccessPointHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ChangeCurrentUserSafeAccessPoint>
{
	public async Task<IResult> Handle(ChangeCurrentUserSafeAccessPoint request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;
		var data = request.Body;

		var safeAccessPoints = await dbContext.UserSafeAccessPoints
			.Where(x => x.UserId == userId && x.Id == request.PointId)
			.FirstOrDefaultAsync(cancellationToken);

		if (safeAccessPoints is null)
			return new ErrorResult("SafeAccessPointNotFound", $"Safe access point with ID `{safeAccessPoints}` not found.", 404);

		safeAccessPoints.Name = data.Name.Trim();
		safeAccessPoints.Latitude = data.Latitude;
		safeAccessPoints.Longitude = data.Longitude;
		safeAccessPoints.Radius = data.Radius;
		safeAccessPoints.EditedAt = DateTime.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(UserSafeAccessPointReadModel.From(safeAccessPoints));
	}
}