using Scalar.AspNetCore;
using Shared.Endpoints;
using Shared.Endpoints.Results;

namespace API.Documentation;

internal static class Extensions
{
	public static void AddDocumentation(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		services.AddOpenApi(options =>
		{
			options.AddSchemaTransformer<RequestBodySchemaTransformer>();
			options.AddOperationTransformer<ResponseBodyOperationTransformer>();
		});
	}

	public static void MapDocumentation(this WebApplication app)
	{
		app.MapOpenApi();
		app.MapScalarApiReference("/docs", options => options
			.WithTitle("CADR API")
			.WithTheme(ScalarTheme.Default)
			.WithDarkMode(true)
			.WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios)
			.WithSidebar(true)
			.WithDownloadButton(true)
			.WithDarkModeToggle(true)
			.WithDotNetFlag(true)
			.WithTestRequestButton(true)
			.WithModels(true)
			.WithDefaultOpenAllTags(false));
	}

}