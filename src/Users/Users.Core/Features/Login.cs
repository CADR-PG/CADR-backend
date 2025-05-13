using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Users.Core.Database;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Core.Features;

internal record Login([FromBody] Login.Credentials Body, HttpContext HttpContext) : IHttpRequest
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
	UsersDbContext dbContext,
	ITokenProvider tokenProvider
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var (email, password) = request.Body;

		var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
		if (user is null || !HashingService.IsValid(password, user.HashedPassword))
			return Results.Problem(statusCode: 400, title: "InvalidLoginCredentials", detail: "Invalid email or password");

		var tokens = tokenProvider.Generate(user);
		request.HttpContext.SetTokenCookies(tokens);

		user.Login(tokens);
		await dbContext.SaveChangesAsync(cancellationToken);

		var readModel = UserReadModel.From(user);
		return Results.Ok(readModel);
	}
}