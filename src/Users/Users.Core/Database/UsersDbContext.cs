using Microsoft.EntityFrameworkCore;
using Users.Core.Entities;

namespace Users.Core.Database;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public required DbSet<User> Users { get; init; }
	public required DbSet<RefreshToken> RefreshTokens { get; init; }

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(UsersModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
		base.OnModelCreating(builder);
	}
}