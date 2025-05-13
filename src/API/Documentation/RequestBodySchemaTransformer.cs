using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Shared.Endpoints;

namespace API.Documentation;

internal class RequestBodySchemaTransformer : IOpenApiSchemaTransformer
{
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		var type = context.JsonTypeInfo.Type;

		if (!type.IsNested || !type.DeclaringType!.IsAssignableTo(typeof(IHttpRequest))) return Task.CompletedTask;

		schema.Title = $"{type.DeclaringType.Name}.{type.Name}";
		schema.Annotations["x-schema-id"] = $"{type.DeclaringType.Name}.{type.Name}";

		return Task.CompletedTask;
	}
}