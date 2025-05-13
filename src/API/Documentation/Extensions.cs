using Scalar.AspNetCore;

namespace API.Documentation;

internal static class Extensions
{
	public static void AddDocumentation(this IServiceCollection services)
		=> services.AddOpenApi(options =>
		{
			options.AddSchemaTransformer<RequestBodySchemaTransformer>();
		});

	public static void MapDocumentation(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapOpenApi();
		endpoints.MapScalarApiReference("/docs", options => options
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