using Microsoft.AspNetCore.Diagnostics;
using Shared.Endpoints.Results;

namespace API.Exceptions;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
	private ErrorResult InternalServerError { get; } = new("InternalServerError", "Please contact the application administrator.", 500);

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		logger.LogError(
			exception, "Exception occurred: {Message}", exception.Message);

		await InternalServerError.ExecuteAsync(httpContext);
		return true;
	}
}