using API.Database.Configurations;
using API.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Database;

internal class CADRDbContext : DbContext
{
	public required DbSet<User> Users { get; init; }

	public CADRDbContext(DbContextOptions<CADRDbContext> options) : base(options)
	{

	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CADRDbContext).Assembly);
	}
}