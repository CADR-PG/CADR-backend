using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules.Users.Features;

internal record Login([FromBody] Login.Credentials Body) : IHttpRequest
{
	internal record Credentials(string Email, string Password);
}

internal record UserReadModel(string response);


internal sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login");
}

internal sealed class LoginHandler(
	ILogger<LoginHandler> logger
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;

		await Task.Delay(1000, cancellationToken);
		logger.LogInformation("Login {EMAIL} - {PASSWORD}", credentials.Email, credentials.Password);
		return Results.Ok(new UserReadModel("Logged in successfully"));
	}
}
