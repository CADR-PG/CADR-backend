using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Users.Core.Database;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct ResendEmailConfirmation(CurrentUser CurrentUser) : IHttpRequest;

internal sealed class ResendEmailConfirmationEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<ResendEmailConfirmation, ResendEmailConfirmationHandler>("resend-email-confirmation")
			.RequireAuthorization()
			.Produces(204)
			.ProducesError(400,
				$"`{SharedErrors.Types.ValidationError}` with details or `{nameof(Errors.ResendConfirmationTimeLimitError)}`")
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Resends email with confirmation link and returns `{nameof(UserReadModel)}`.");
}

internal sealed class ResendEmailConfirmationHandler(
	UsersDbContext dbContext,
	EmailConfirmationService emailConfirmationService
) : IHttpRequestHandler<ResendEmailConfirmation>
{
	public async Task<IResult> Handle(ResendEmailConfirmation request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.FirstAsync(x => x.Id == request.CurrentUser.Id, cancellationToken);

		if (user.EmailConfirmation.SentAt > DateTime.UtcNow.AddMinutes(-1))
			return Errors.ResendConfirmationTimeLimitError;

		user.SetupEmailConfirmation();
		await dbContext.SaveChangesAsync(cancellationToken);

		await emailConfirmationService.SendEmailConfirmation(user);

		return Results.NoContent();
	}
}