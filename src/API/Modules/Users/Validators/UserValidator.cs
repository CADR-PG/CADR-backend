using API.Modules.Users.Models;
using FluentValidation;

namespace API.Modules.Users.Validators;

internal class UserValidator : AbstractValidator<User>
{
	public UserValidator()
	{
		RuleFor(x => x.FirstName)
			.NotEmpty()
			.WithMessage("First name is required")
			.MaximumLength(50)
			.WithMessage("First name must be less than 50 characters");
		RuleFor(x => x.LastName)
			.NotEmpty()
			.WithMessage("Last name is required")
			.MaximumLength(50)
			.WithMessage("Last name must be less than 50 characters");
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email is required")
			.EmailAddress()
			.WithMessage("Email is not valid");
		RuleFor(x => x.PhoneNumber)
			.NotEmpty()
			.WithMessage("Phone number is required")
			.Matches(@"^\+?[1-9]\d{1,14}$")
			.WithMessage("Phone number is not valid");
		RuleFor(x => x.PasswordHash)
			.NotEmpty()
			.WithMessage("Password is required")
			.MinimumLength(8)
			.WithMessage("Password must be at least 8 characters long");
	}
}
