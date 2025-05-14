using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Shared.Endpoints.Results;

namespace API.Documentation;

internal sealed class ResponseBodyOperationTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
		CancellationToken cancellationToken)
	{
		var metadata = context.Description
			.ActionDescriptor
			.EndpointMetadata
			.OfType<ResponseDescriptionMetadata>()
			.ToList();

		operation.Responses.ToList().ForEach(response =>
		{
			var content = response.Value.Content;
			if (content.Count == 0) return;
			var schemaName = content.First().Value.Schema.Annotations["x-schema-id"].ToString();
			response.Value.Description = schemaName;
		});

		metadata.ForEach(descriptionMetadata =>
		{
			var response = operation.Responses[$"{descriptionMetadata.StatusCode}"];
			response.Description = $"{response.Description}:  {descriptionMetadata.Description}";
		});

		return Task.CompletedTask;
	}
}