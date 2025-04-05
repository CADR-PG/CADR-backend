using API.Database.Configurations;
using API.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Database;

internal class CADRDbContext : DbContext
{
	public required DbSet<User> Users { get; set; }

	public CADRDbContext(DbContextOptions<CADRDbContext> options) : base(options)
	{

	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		new UserConfiguration().Configure(modelBuilder.Entity<User>());
	}
}
