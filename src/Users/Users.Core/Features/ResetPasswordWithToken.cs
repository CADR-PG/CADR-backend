using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Users.Core.Database;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct ResetPasswordWithToken([FromQuery] ResetPasswordWithToken.Credentials Body) : IHttpRequest
{
	internal record Credentials(Guid Token, string Email, string Password);
}

internal sealed class ResetPasswordWithTokenEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<ResetPasswordWithToken, ResetPasswordWithTokenHandler>("reset-password")
			.AllowAnonymous()
			.Produces(204)
			.ProducesError(400, $"`{nameof(SharedErrors.ValidationError)}` with details or `{Errors.InvalidPasswordResetToken.Type}`")
			.WithDescription("Sends email with reset password token, when given email is valid.");
}

internal sealed class ResetPasswordWithTokenHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ResetPasswordWithToken>
{
	public async Task<IResult> Handle(ResetPasswordWithToken request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.FirstOrDefaultAsync(x => x.Email == request.Body.Email && x.PasswordResetToken == request.Body.Token, cancellationToken);

		if (user is null || user.PasswordResetExpiresAt > DateTime.UtcNow.AddHours(-12)) return Errors.InvalidPasswordResetToken;

		user.PasswordResetToken = null;
		user.PasswordResetExpiresAt = null;
		user.HashedPassword = HashingService.Hash(request.Body.Password);

		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}