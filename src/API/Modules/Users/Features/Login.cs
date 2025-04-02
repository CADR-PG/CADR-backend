using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules.Users.Features;

public record Login([FromBody] Login.Credentials Body) : IHttpRequest
{
	public record Credentials(string Email, string Password);
}
public record UserReadModel(string Email);


public sealed class LoginEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints)
		=> endpoints.MapPost<Login, LoginHandler>("login");
}

public sealed class LoginHandler(
	ILogger<LoginHandler> logger
) : IHttpRequestHandler<Login>
{
	public async Task<IResult> Handle(Login request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;

		await Task.Delay(1000, cancellationToken);
		logger.LogInformation("Login {EMAIL} - {PASSWORD}", credentials.Email, credentials.Password);
		return Results.Ok(new UserReadModel(credentials.Email));
	}
}
