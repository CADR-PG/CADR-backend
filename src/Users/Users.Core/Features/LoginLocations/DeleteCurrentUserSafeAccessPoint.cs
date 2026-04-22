using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Users.Core.Database;

namespace Users.Core.Features.LoginLocations;

internal record struct DeleteCurrentUserSafeAccessPoint(CurrentUser CurrentUser, [FromRoute] Guid PointId) : IHttpRequest;

internal sealed class DeleteCurrentUserSafeAccessPointEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapDelete<DeleteCurrentUserSafeAccessPoint, DeleteCurrentUserSafeAccessPointHandler>("safe-access-points/{pointId}")
			.Produces(204)
			.RequireAuthorization()
			.WithDescription("Deletes the user safe access point.");
}

internal sealed class DeleteCurrentUserSafeAccessPointHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<DeleteCurrentUserSafeAccessPoint>
{
	public async Task<IResult> Handle(DeleteCurrentUserSafeAccessPoint request, CancellationToken cancellationToken)
	{
		var userId = request.CurrentUser.Id;

		await dbContext.UserSafeAccessPoints
			.Where(x => x.UserId == userId && x.Id == request.PointId)
			.ExecuteDeleteAsync(cancellationToken);

		return Results.NoContent();
	}
}