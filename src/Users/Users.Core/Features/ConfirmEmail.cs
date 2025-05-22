using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using Users.Core.Database;
using Users.Core.ReadModels;

namespace Users.Core.Features;

internal record struct ConfirmEmail([FromQuery] Guid UserId, [FromQuery] int Code) : IHttpRequest
{
	public record Data(string CurrentPassword, string NewPassword);
}

internal sealed class ConfirmEmailEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<ConfirmEmail, ConfirmEmailHandler>("confirm-email")
			.Produces(204)
			.ProducesError(400, $"`{nameof(Errors.InvalidEmailConfirmationCredentialsError)}`")
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Changes current user password. Returns `{nameof(UserReadModel)}`.")
			.AddValidation<ChangePassword.Data>();
}

internal sealed class ConfirmEmailHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ConfirmEmail>
{
	public async Task<IResult> Handle(ConfirmEmail request, CancellationToken cancellationToken)
	{
		var userId = new UserId(request.UserId);

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

		if (user is null || !user.ConfirmEmail(request.Code))
			return Errors.InvalidEmailConfirmationCredentialsError;

		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}