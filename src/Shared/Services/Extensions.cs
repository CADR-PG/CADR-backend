using IdGen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shared.Settings;

namespace Shared.Services;

public static class Extensions
{
	public static void AddMailingService(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSettingsWithOptions<MailingSettings>(configuration);
		services.TryAddScoped<IMailingService, MailingService>();
	}

	public static void AddTimeProvider(this IServiceCollection services)
	{
		services.TryAddSingleton(TimeProvider.System);
	}

	public static void AddSnowflakeIdProvider(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddTimeProvider();
		services.AddSettingsWithOptions<SnowflakeIdSettings>(configuration);
		services.TryAddSingleton<IIdGenerator<long>>(serviceProvider => SnowflakeIdProvider.CreateGenerator(
			serviceProvider.GetRequiredService<IOptions<SnowflakeIdSettings>>(),
			serviceProvider.GetRequiredService<TimeProvider>()));
		services.TryAddSingleton<ISnowflakeIdProvider, SnowflakeIdProvider>();
	}
}
