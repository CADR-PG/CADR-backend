using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities.Teams;

namespace Projects.Core.Database.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
	public void Configure(EntityTypeBuilder<Team> builder)
	{
		builder.HasKey(t => t.Id);

		builder.Property(t => t.Id).ValueGeneratedOnAdd();

		builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
		builder.Property(t => t.Description).IsRequired(false);

		builder.Property(t => t.CreatedBy).IsRequired();
		builder.Property(t => t.CreatedAt).IsRequired();
		builder.Property(t => t.UpdatedAt).IsRequired(false);
	}
}
