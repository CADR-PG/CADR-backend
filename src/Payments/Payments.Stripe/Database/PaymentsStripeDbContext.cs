using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Payments.Stripe.Database;

internal sealed class PaymentsStripeDbContext(DbContextOptions<PaymentsStripeDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(PaymentsStripeModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(PaymentsStripeDbContext).Assembly);
		builder.ApplyConfigurationsFromAssembly(typeof(UserReference).Assembly);
		base.OnModelCreating(builder);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder
			.Properties<UserId>()
			.HaveConversion<UserIdConverter>();
	}
}