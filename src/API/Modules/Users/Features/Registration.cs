using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules.Users.Features;

internal record Registration([FromBody] Registration.Credentials Body) : IHttpRequest
{
	internal record Credentials(
		string FirstName, string LastName,
		string Email, string Password,
		string PhoneNumber, DateTime BirthDate);
};

internal record RegistrationReadModel(string response);

internal sealed class RegistrationEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Registration, RegistrationHandler>("/register");
}

internal sealed class RegistrationHandler(
	CADRDbContext dbContext
) : IHttpRequestHandler<Registration>
{
	public async Task<IResult> Handle(Registration request, CancellationToken cancellationToken)
	{
		var credentials = request.Body;

		var passwordHasher = new PasswordHasher<User>();
		var user = new User
		{
			FirstName = credentials.FirstName.Trim(),
			LastName = credentials.LastName.Trim(),
			Email = credentials.Email.Trim(),
			Phone = credentials.PhoneNumber.Trim(),
			BirthDate = credentials.BirthDate,
			Password = credentials.Password.Trim(),
		};
		user.Password = passwordHasher.HashPassword(user, credentials.Password);
		dbContext.Users.Add(user);
		await dbContext.SaveChangesAsync(cancellationToken);
		return Results.Ok(new RegistrationReadModel("Registered successfully"));
	}
}