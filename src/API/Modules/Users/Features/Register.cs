using API.Database;
using API.Modules.Users.Entities;
using API.Modules.Users.Services;
using API.Shared.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Modules.Users.Features;

internal record Register([FromBody] Register.Credentials Body) : IHttpRequest
{
	internal record Credentials(string FirstName, string LastName, string Email, string Password)
	{
		public string Email { get; } = Email.ToLower();
	}
};

internal sealed class RegisterEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) =>
		endpoints.MapPost<Register, RegisterHandler>("/register")
			.ProducesProblem(400)
			.ProducesValidationProblem();
}

internal sealed class RegisterHandler(
	CADRDbContext dbContext
) : IHttpRequestHandler<Register>
{
	public async Task<IResult> Handle(Register request, CancellationToken cancellationToken)
	{
		var (firstName, lastName, email, password) = request.Body;

		var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => email == u.Email.ToLower(), cancellationToken);
		if (existingUser is not null) return Results.Problem(statusCode: 400, title: "EmailAlreadyExists", detail: $"A user with email `{email}` already exists.");

		var user = new User
		{
			Id = Guid.NewGuid(),
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			HashedPassword = HashingService.Hash(password),
		};

		await dbContext.Users.AddAsync(user, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Results.NoContent();
	}
}

internal sealed class RegisterValidator : AbstractValidator<Register>
{
	public RegisterValidator()
	{
		RuleFor(x => x.Body.FirstName)
			.NotEmpty()
			.WithMessage("First name is required")
			.MaximumLength(50)
			.WithMessage("First name must be less than 50 characters");
		RuleFor(x => x.Body.LastName)
			.NotEmpty()
			.WithMessage("Last name is required")
			.MaximumLength(50)
			.WithMessage("Last name must be less than 50 characters");
		RuleFor(x => x.Body.Email)
			.NotEmpty().WithMessage("Email is required")
			.EmailAddress().WithMessage("Email is not valid");
		RuleFor(x => x.Body.Password)
			.NotEmpty().WithMessage("Password is required")
			.MinimumLength(8).WithMessage("Password must be at least 8 characters long");
	}
}