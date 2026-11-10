using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payments.Stripe.Database;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Settings;
using Stripe;
using System.Runtime.CompilerServices;

namespace Payments.Stripe;

public class PaymentsStripeModule : IModule
{
	public static string Name => "PaymentsStripe";

	public void Register(IHostApplicationBuilder builder)
	{
		var services = builder.Services;
		var configuration = builder.Configuration;

		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		var client = new StripeClient();
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		var payments = endpoints.MapGroup(Name.ToLowerInvariant()).WithTags(Name);
	}

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<PaymentsStripeDbContext>();
		await dbContext.Database.MigrateAsync();
	}
}