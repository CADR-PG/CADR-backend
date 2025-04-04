using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace API.Modules.Users.Features;

internal record Logout([FromBody] Logout.Credentials Body) : IHttpRequest
{
	internal record Credentials(
		string FirstName, string LastName,
		string Email, string Password,
		string PhoneNumber, DateTime BirthDate);
};

internal record LogoutReadModel(string response);

internal sealed class LogoutEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Logout, LogoutHandler>("/logout");
}

internal sealed class LogoutHandler(
	CADRDbContext dbContext
) : IHttpRequestHandler<Logout>
{
	public async Task<IResult> Handle(Logout request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;

		var passwordHasher = new PasswordHasher<User>();
		var user = new User
		{
			FirstName = credentials.FirstName,
			LastName = credentials.LastName,
			Email = credentials.Email,
			Phone = credentials.PhoneNumber,
			BirthDate = credentials.BirthDate,
			Password = credentials.Password,
		};
		user.Password = passwordHasher.HashPassword(user, credentials.Password);
		dbContext.Users.Add(user);
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(new LogoutReadModel("Logged out successfully"));
	}
}