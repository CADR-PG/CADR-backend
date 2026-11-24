using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payments.Stripe.Database;
using Payments.Stripe.Entities;
using Payments.Stripe.Features;
using Payments.Stripe.Settings;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Settings;
using Stripe;
using System.Runtime.CompilerServices;
using Users.Contracts.Database.References;

namespace Payments.Stripe;

public class PaymentsStripeModule : IModule
{
	public static string Name => "Payments-Stripe";

	public void Register(IHostApplicationBuilder builder)
	{
		var services = builder.Services;
		var configuration = builder.Configuration;

		var postgreSqlSettings = configuration.GetSettings<PostgreSqlSettings>();
		services.AddDbContext<PaymentsStripeDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", Name)));

		var stripeClientSettings = configuration.GetSettings<StripeClientSettings>();
		var client = new StripeClient(stripeClientSettings.SecretApiKey);
		services.AddSingleton<IStripeClient>(client);
		services.AddScoped<CreateCustomerHandler>();
		services.AddScoped<PayHandler>();
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		var payments = endpoints.MapGroup(Name.ToLowerInvariant()).WithTags(Name);
		payments.Map<CreateCustomerEndpoint>();
		payments.Map<PayEndpoint>();
	}

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<PaymentsStripeDbContext>();
		await dbContext.Database.MigrateAsync();
	}
}