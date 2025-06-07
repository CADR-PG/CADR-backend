using Microsoft.AspNetCore.Http;
using Shared.Endpoints.Results;

namespace Users.Core;

public static class Errors
{
	public static ErrorResult InvalidLoginCredentialsError => new(nameof(InvalidLoginCredentialsError), "Invalid email or password.");
	public static ErrorResult InvalidRefreshCredentialsError => new(nameof(InvalidRefreshCredentialsError), "Missing or invalid refresh token.");
	public static ErrorResult EmailAlreadyTakenError => new(nameof(EmailAlreadyTakenError), "The email is already associated with another user.");
	public static ErrorResult ProjectWithThisNameAlreadyExists => new(nameof(ProjectWithThisNameAlreadyExists), "The project with this name already exists.");
}