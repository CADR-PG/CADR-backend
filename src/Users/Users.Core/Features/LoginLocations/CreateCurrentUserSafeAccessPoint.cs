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

internal record struct CreateCurrentUserSafeAccessPoint(CurrentUser CurrentUser, [FromBody] CreateCurrentUserSafeAccessPoint.Data Body) : IHttpRequest
{
	public sealed record Data
	{
		public required string Name { get; set; }

		public required double Latitude { get; set; }
		public required double Longitude { get; set; }

		public required double Radius { get; set; }
	};
}

internal sealed class CreateCurrentUserSafeAccessPointEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<CreateCurrentUserSafeAccessPoint, CreateCurrentUserSafeAccessPointHandler>("safe-access-points")
			.Produces<UserSafeAccessPointReadModel>()
			.ProducesError(400, $"`SafeAccessPointsLimitReached` (max. 5)")
			.ProducesError(409, $"`SafeAccessPointAlreadyExists`")
			.RequireAuthorization()
			.WithDescription("Changes and returns the user safe access point for given ID.");
}

internal sealed class CreateCurrentUserSafeAccessPointHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<CreateCurrentUserSafeAccessPoint>
{
	public async Task<IResult> Handle(CreateCurrentUserSafeAccessPoint request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;
		var data = request.Body;

		var safeAccessPointsCount = await dbContext.UserSafeAccessPoints
			.Where(x => x.UserId == userId)
			.CountAsync(cancellationToken);

		if (safeAccessPointsCount >= 5)
			return new ErrorResult("SafeAccessPointLimitReached", $"The maximum number of safe access points is 5.");

		var safeAccessPointsWithGivenNameExists = await dbContext.UserSafeAccessPoints
			.AnyAsync(x => x.UserId == userId && x.Name == data.Name, cancellationToken);

		if (safeAccessPointsWithGivenNameExists)
			return new ErrorResult("SafeAccessPointLimitReached", $"Safe access point with given name `{data.Name}` already exists.", 409);

		var safeAccessPoints = new UserSafeAccessPoint
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			Name = data.Name.Trim(),
			CreatedAt = DateTime.UtcNow,
			EditedAt = null,
			Latitude = data.Latitude,
			Longitude = data.Longitude,
			Radius = data.Radius
		};

		dbContext.UserSafeAccessPoints.Add(safeAccessPoints);
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(UserSafeAccessPointReadModel.From(safeAccessPoints));
	}
}