using Shared.Endpoints.Results;

namespace Shared.Endpoints;

public static class SharedErrors
{
	public static class Types
	{
		public const string ValidationError = nameof(ValidationError);
		public const string UnauthorizedError = nameof(UnauthorizedError);
	}

	public static ErrorResult UnauthorizedError => new(Types.UnauthorizedError, "Missing or invalid credentials.", 401);
	public static ErrorResult ValidationError(IEnumerable<ErrorResultDetail>? details = null) => new(Types.ValidationError, "Validation error.") { Details = details ?? [] };
}