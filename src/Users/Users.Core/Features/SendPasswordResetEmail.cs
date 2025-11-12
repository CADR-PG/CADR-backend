using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Users.Core.Database;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct SendPasswordResetEmail([FromQuery] SendPasswordResetEmail.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email);
}

internal sealed class SendPasswordResetEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<SendPasswordResetEmail, SendPasswordResetHandler>("reset-password")
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
		user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(12);

		await dbContext.SaveChangesAsync(cancellationToken);

		await userMailingService.SendResetPassword(user);

		return Results.NoContent();
	}
}