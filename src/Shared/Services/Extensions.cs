using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Settings;

namespace Shared.Services;

public static class Extensions
{
	public static void AddMailingService(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSettingsWithOptions<MailingSettings>(configuration);
		services.TryAddScoped<IMailingService, MailingService>();
	}
}