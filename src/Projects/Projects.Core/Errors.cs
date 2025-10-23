using Shared.Endpoints.Results;

namespace Projects.Core;

public static class Errors
{
	public static ErrorResult ProjectWithThisNameAlreadyExists => new(nameof(ProjectWithThisNameAlreadyExists), "The project with this name already exists.");
}