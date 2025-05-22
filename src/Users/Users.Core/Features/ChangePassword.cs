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

internal record struct ChangePassword([FromBody] ChangePassword.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	public record Data(string CurrentPassword, string NewPassword);
}

internal sealed class ChangePasswordEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapGet<ChangePassword, ChangePasswordHandler>("change-password")
			.RequireAuthorization()
			.Produces<UserReadModel>()
			.ProducesError(400, $"{nameof(SharedErrors.ValidationError)}` with details or `{nameof(Errors.InvalidCurrentPasswordError)}`")
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Changes current user password. Returns `{nameof(UserReadModel)}`.")
			.AddValidation<ChangePassword.Data>();
}

internal sealed class ChangePasswordHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ChangePassword>
{
	public async Task<IResult> Handle(ChangePassword request, CancellationToken cancellationToken)
	{
		var (currentPassword, newPassword) = request.Body;

		var user = await dbContext.Users
			.Include(x => x.RefreshTokens)
			.FirstOrDefaultAsync(x => x.Email == currentPassword, cancellationToken);

		if (user is null || !HashingService.IsValid(currentPassword, user.HashedPassword))
			return Errors.InvalidCurrentPasswordError;

		user.HashedPassword = HashingService.Hash(newPassword);

		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePassword.Data>
{
	public ChangePasswordValidator()
	{
		RuleFor(x => x.NewPassword)
			.MinimumLength(8);
	}
}