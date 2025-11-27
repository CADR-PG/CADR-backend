using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Users.Core.Database;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct SendPasswordResetEmail([FromBody] SendPasswordResetEmail.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email);
}

internal sealed class SendPasswordResetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<SendPasswordResetEmail, SendPasswordResetHandler>("send-reset-password-email")
			.AllowAnonymous()
			.Produces(204)
			.WithDescription("Sends email with reset password token, when given email is valid.");
}

internal sealed class SendPasswordResetHandler(
	UsersDbContext dbContext,
	UserMailingService userMailingService
) : IHttpRequestHandler<SendPasswordResetEmail>
{
	public async Task<IResult> Handle(SendPasswordResetEmail request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Email == request.Body.Email, cancellationToken);

		if (user is null) return Results.NoContent();

		user.PasswordResetToken = Guid.NewGuid();
		user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);

		await dbContext.SaveChangesAsync(cancellationToken);

		// TODO xxx
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		userMailingService.SendResetPassword(user);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

		return Results.NoContent();
	}
}