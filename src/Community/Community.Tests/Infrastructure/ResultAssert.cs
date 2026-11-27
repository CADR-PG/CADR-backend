using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Endpoints.Results;

namespace Community.Tests.Infrastructure;

internal static class ResultAssert
{
	public static T Ok<T>(IResult result)
	{
		var ok = Assert.IsType<Ok<T>>(result, exactMatch: false);
		return Assert.IsType<T>(ok.Value, exactMatch: false);
	}

	public static T Created<T>(IResult result)
	{
		var created = Assert.IsType<Created<T>>(result, exactMatch: false);
		return Assert.IsType<T>(created.Value, exactMatch: false);
	}

	public static T Success<T>(IResult result)
	{
		var valueResult = Assert.IsAssignableFrom<IValueHttpResult<T>>(result);
		Assert.InRange(Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode ?? 0, 200, 299);
		return Assert.IsType<T>(valueResult.Value, exactMatch: false);
	}

	public static void NoContent(IResult result)
	{
		if (result is ErrorResult error)
			Assert.Fail($"Expected 204 NoContent, got {error.StatusCode} {error.Type}: {error.Message}");

		Assert.IsType<NoContent>(result);
	}

	public static ErrorResult Error(IResult result, string expectedType, int expectedStatusCode)
	{
		var error = Assert.IsType<ErrorResult>(result);
		Assert.Equal(expectedType, error.Type);
		Assert.Equal(expectedStatusCode, error.StatusCode);
		return error;
	}
}