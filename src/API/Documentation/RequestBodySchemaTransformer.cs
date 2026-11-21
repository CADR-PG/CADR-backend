using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shared.Endpoints;

namespace API.Documentation;

internal class RequestBodySchemaTransformer : IOpenApiSchemaTransformer
{
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		var type = context.JsonTypeInfo.Type;

		if (!type.IsNested || !type.DeclaringType!.IsAssignableTo(typeof(IHttpRequest))) return Task.CompletedTask;

		schema.Title = $"{type.DeclaringType.Name}.{type.Name}";

		return Task.CompletedTask;
	}
}