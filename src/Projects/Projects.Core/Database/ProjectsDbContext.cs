using Microsoft.EntityFrameworkCore;
using Projects.Core.Entities;
using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Projects.Core.Database;

internal sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public DbSet<Project> Projects { get; init; } = null!;

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(ProjectsModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
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