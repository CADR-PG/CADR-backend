using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Endpoints.Results;

public record ResponseDescriptionMetadata(Type Type, int StatusCode, string Description);

public static class Extensions
{
	public static TBuilder ProducesError<TBuilder>(this TBuilder builder, int statusCode, string description) where TBuilder : IEndpointConventionBuilder
	{
		builder.WithMetadata(new ProducesResponseTypeAttribute(typeof(ErrorResult), statusCode, "application/json"));
		builder.WithMetadata(new ResponseDescriptionMetadata(typeof(ErrorResult), statusCode, description));

		return builder;
	}
}