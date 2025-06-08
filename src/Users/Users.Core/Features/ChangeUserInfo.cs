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

namespace Users.Core.Features;

internal record struct ChangeUserInfo([FromBody] ChangeUserInfo.Data Body, CurrentUser CurrentUser) : IHttpRequest
{
	public record Data(string FirstName, string LastName);
}

internal sealed class ChangeUserInfoEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPut<ChangeUserInfo, ChangeUserInfoHandler>("change-info")
			.RequireAuthorization()
			.Produces<UserReadModel>()
			.ProducesError(401, "`UnauthorizedError`")
			.WithDescription($"Changes current user basic info. Returns `{nameof(UserReadModel)}`.")
			.AddValidation<ChangeUserInfo.Data>();
}

internal sealed class ChangeUserInfoHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<ChangeUserInfo>
{
	public async Task<IResult> Handle(ChangeUserInfo request, CancellationToken cancellationToken)
	{
		var user = await dbContext.Users
			.FirstAsync(x => x.Id == request.CurrentUser.Id, cancellationToken);

		user.FirstName = request.Body.FirstName;
		user.LastName = request.Body.LastName;

		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}

internal sealed class ChangeUserInfoValidator : AbstractValidator<ChangeUserInfo.Data>
{
	public ChangeUserInfoValidator()
	{
		RuleFor(x => x.FirstName).NotEmpty();
		RuleFor(x => x.LastName).NotEmpty();
	}
}