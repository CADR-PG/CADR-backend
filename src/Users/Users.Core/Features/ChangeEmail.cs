using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Users.Core.Database;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record struct ChangeEmail([FromBody] ChangeEmail.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	public record Data(string NewEmail);
}

internal sealed class ChangeEmailEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<ChangeEmail, ChangeEmailHandler>("change-change-email")
			.RequireAuthorization()
			.Produces<UserReadModel>()
			.ProducesError(400, $"`{SharedErrors.Types.ValidationError}` with details or `{nameof(Errors.EmailAlreadyTakenError)}`")
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Changes current user email. Sends email with confirmation link and returns `{nameof(UserReadModel)}`.")
			.AddValidation<ChangeEmail.Data>();
}

internal sealed class ChangeEmailHandler(
	UsersDbContext dbContext,
	EmailConfirmationService emailConfirmationService
) : IHttpRequestHandler<ChangeEmail>
{
	public async Task<IResult> Handle(ChangeEmail request, CancellationToken cancellationToken)
	{
		var newEmail = request.Body.NewEmail.ToLowerInvariant();

		var emailAlreadyTaken = await dbContext.Users.AnyAsync(u => newEmail == u.Email, cancellationToken);
		if (emailAlreadyTaken) return Errors.EmailAlreadyTakenError;

		var user = await dbContext.Users
			.FirstAsync(x => x.Id == request.CurrentUser.Id, cancellationToken);

		user.Email = newEmail;
		user.SetupEmailConfirmation();

		await dbContext.SaveChangesAsync(cancellationToken);
		await emailConfirmationService.SendEmailConfirmation(user);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class ChangeEmailValidator : AbstractValidator<ChangeEmail.Data>
{
	public ChangeEmailValidator()
	{
		RuleFor(x => x.NewEmail).MinimumLength(8);
	}
}