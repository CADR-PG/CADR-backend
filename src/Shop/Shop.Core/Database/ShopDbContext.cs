using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
// using Shop.Core.Entities;
using Shared.ValueObjects;
using Shop.Core.Entities.Catalog;
using Shop.Core.Entities.Funds;
using Shop.Core.Entities.Library;
using Shop.Core.Entities.Shopping.Order;
using Shop.Core.Entities.Users;
using Users.Contracts.Database.References;

namespace Shop.Core.Database;

internal sealed class ShopDbContext(DbContextOptions<ShopDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public DbSet<Game> Games { get; init; } = null!;
	public DbSet<Genre> Genres { get; init; } = null!;

	public DbSet<Price> Prices { get; init; } = null!;
	public DbSet<LibraryEntry> LibraryEntries { get; init; } = null!;
	public DbSet<GameVersion> GameVersions { get; init; } = null!;
	public DbSet<Order> Orders { get; init; } = null!;
	public DbSet<ShopUser> ShopUsers { get; init; } = null!;

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