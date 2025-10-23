using Microsoft.EntityFrameworkCore;
using Shared.Modules;
using Projects.Core.Entities;

namespace Projects.Core.Database;

internal sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public DbSet<Project> Projects { get; init; } = null!;


	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(ProjectsModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
		base.OnModelCreating(builder);
	}

	public static string Schema => "users";
}