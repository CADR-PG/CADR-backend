using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.ReadModels;

namespace Users.Core.Features.LoginLocations;

internal record struct GetCurrentUserLocationLogs(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class GetCurrentUserLocationLogsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<GetCurrentUserLocationLogs, GetCurrentUserLocationLogsHandler>("location-logs")
			.Produces<UserLocationLogsReadModel>()
			.RequireAuthorization()
			.WithDescription($"Returns user login locations. Returns `{nameof(UserLocationLogsReadModel)}` on success.");
}

internal sealed class GetCurrentUserLocationLogsHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<GetCurrentUserLocationLogs>
{
	public async Task<IResult> Handle(GetCurrentUserLocationLogs request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;

		var logs = await dbContext.UserLocationLogs
			.Where(ull => ull.UserId == userId)
			.OrderByDescending(ull => ull.OccuredAt)
			.ToListAsync(cancellationToken);

		var entries = logs.Select((ull, i) =>
		{
			var distanceKm = i == 0 ? 0 : UserLocationLog.CalculateDistanceKm(logs[i - 1], ull);
			var isDistanceIssue = i != 0 && UserLocationLog.IsDistanceIssue(logs[i - 1], ull, distanceKm);

			return new UserLocationLogsReadModel.Entry
			{
				AuthenticationType = ull.AuthenticationType,
				OccuredAt = ull.OccuredAt,
				IpAddress = ull.IpAddress,
				Latitude = ull.Latitude,
				Longitude = ull.Longitude,
				Country = ull.Country,
				City = ull.City,
				DistanceFromLastLocationKm = distanceKm,
				IsDistanceIssue = isDistanceIssue
			};
		});

		return Results.Ok(new UserLocationLogsReadModel { Logs = entries });
	}
}