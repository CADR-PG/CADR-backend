using Microsoft.EntityFrameworkCore;
using Users.Core.Entities;

namespace Users.Core.Database;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public DbSet<User> Users { get; init; } = null!;
	public DbSet<RefreshToken> RefreshTokens { get; init; } = null!;
	public DbSet<Project> Projects { get; init; } = null!;


	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(UsersModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
		base.OnModelCreating(builder);
	}
}