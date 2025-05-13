using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Exceptions;

namespace Shared.Settings;

public static class Extensions
{
	public static T GetSettings<T>(this IConfiguration configuration) where T : ISettings
		=> configuration.GetSection(T.SectionName).Get<T>()
		   ?? throw new CadrException($"Invalid configuration section `{T.SectionName}`");

	public static void AddSettingsWithOptions<T>(this IServiceCollection services) where T : class, ISettings
		=> services.AddOptions<T>(T.SectionName);
}