using API.Database;
using API.Modules.Users.Entities;
using API.Modules.Users.ReadModels;
using API.Modules.Users.Services;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Modules.Users.Features;

internal record Login([FromBody] Login.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email, string Password);
}

internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login")
			.Produces<UserReadModel>()
			.WithDescription("Register a user with name, email and password");
}

internal sealed class LoginHandler(
	CADRDbContext dbContext,
	IUserTokensProvider userTokensProvider,
	UserTokensHttpStorage userTokensHttpStorage
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var (email, password) = request.Body;

		var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.Trim(), cancellationToken);
		if (user is null || !HashingService.IsValid(password, user.HashedPassword))
			return Results.Problem(statusCode: 400, title: "InvalidLoginCredentials", detail: "Invalid email or password");

		var tokens = userTokensProvider.Generate(user);
		userTokensHttpStorage.Set(tokens);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}