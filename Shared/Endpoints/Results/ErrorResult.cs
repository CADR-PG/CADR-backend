using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Shared.Endpoints.Results;

public record ErrorResult(string Type, string Message, [property: JsonIgnore] int StatusCode = 400) : IResult
{
	public IEnumerable<ErrorResultDetail> Details { get; init; } = [];

	public Task ExecuteAsync(HttpContext httpContext)
	{
		httpContext.Response.StatusCode = StatusCode;
		httpContext.Response.WriteAsJsonAsync(this);

		return Task.CompletedTask;
	}
}

public record ErrorResultDetail(string Context, string Message);