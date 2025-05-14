using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.Services;

namespace Users.Core.Features;


internal sealed record Register([FromBody] Register.Credentials Body) : IHttpRequest
{
	internal sealed record Credentials(string FirstName, string LastName, string Email, string Password);
}

internal sealed class RegisterEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Register, RegisterHandler>("/register")
			.Produces(204)
			.ProducesError(400, $"`ValidationError` with details or `{nameof(Errors.EmailAlreadyTakenError)}`")
			.WithDescription("Register new user. User will receive an email with activation link.")
			.AddValidation<Register.Credentials>();
}

internal sealed class RegisterHandler(
	UsersDbContext dbContext
) : IHttpRequestHandler<Register>
{
	public async Task<IResult> Handle(Register request, CancellationToken cancellationToken)
	{
		var (firstName, lastName, email, password) = request.Body;

		var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => email == u.Email, cancellationToken);
		if (existingUser is not null) return Errors.EmailAlreadyTakenError;

		var user = new User
		{
			Id = Guid.NewGuid(),
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			HashedPassword = HashingService.Hash(password),
			LastLoggedInAt = DateTime.UtcNow,
			RefreshTokens = []
		};

		await dbContext.Users.AddAsync(user, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class RegisterValidator : AbstractValidator<Register.Credentials>
{
	public RegisterValidator()
	{
		RuleFor(x => x.Email).NotEmpty().EmailAddress();
		RuleFor(x => x.FirstName).NotEmpty();
		RuleFor(x => x.LastName).NotEmpty();
		RuleFor(x => x.Password).MinimumLength(8);
	}
}