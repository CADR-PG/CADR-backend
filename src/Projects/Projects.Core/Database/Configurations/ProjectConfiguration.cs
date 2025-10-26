using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities;

namespace Projects.Core.Database.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
	public void Configure(EntityTypeBuilder<Project> builder)
	{
		builder.HasKey(p => p.Id);
		builder.Property(p => p.Id).ValueGeneratedOnAdd();
		builder.Property(p => p.UserId).IsRequired();
		builder.Property(p => p.Name).IsRequired();
		builder.Property(p => p.Description).IsRequired();
		builder.Property(p => p.JsonDocument);

		builder.HasOne(x => x.User)
			.WithMany()
			.HasPrincipalKey(ur => ur.Id)
			.HasForeignKey(p => p.UserId)
			.IsRequired();
	}
}