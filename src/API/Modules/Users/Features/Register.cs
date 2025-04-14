using API.Database;
using API.Modules.Users.Models;
using API.Shared.Endpoints;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules.Users.Features;

internal record Register([FromBody] Register.Credentials Body) : IHttpRequest
{
	internal record Credentials(
		string FirstName, string LastName,
		string Email, string Password,
		string PhoneNumber);
};

internal record RegisterReadModel(string response);

internal sealed class RegisterEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Register, RegisterHandler>("/register");
}

internal sealed class RegisterHandler(
	CADRDbContext dbContext, IValidator<User> validator) : IHttpRequestHandler<Register>
{
	public async Task<IResult> Handle(Register request, CancellationToken cancellationToken)
{
	var credentials = request.Body;

	var passwordHasher = new PasswordHasher<User>();

	var user = new User
	{
		FirstName = credentials.FirstName.Trim(),
		LastName = credentials.LastName.Trim(),
		Email = credentials.Email.Trim(),
		PhoneNumber = credentials.PhoneNumber.Trim(),
		PasswordHash = credentials.Password.Trim(),
	};

	ValidationResult validationResult = await validator.ValidateAsync(user, cancellationToken);
	if (!validationResult.IsValid)
		return Results.ValidationProblem(validationResult.ToDictionary());

	user.PasswordHash = passwordHasher.HashPassword(user, credentials.Password);
	dbContext.Users.Add(user);
	await dbContext.SaveChangesAsync(cancellationToken);
	return Results.Ok(new RegisterReadModel("Registered successfully"));
}
}
