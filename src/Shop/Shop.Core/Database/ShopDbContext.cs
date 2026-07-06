using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
// using Shop.Core.Entities;
using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Shop.Core.Database;

internal sealed class ShopDbContext(DbContextOptions<ShopDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(ShopModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
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