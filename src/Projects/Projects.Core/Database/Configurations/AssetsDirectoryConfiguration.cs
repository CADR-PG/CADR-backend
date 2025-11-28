using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities;
using Project = Projects.Core.Entities.Project;

namespace Projects.Core.Database.Configurations;

internal sealed class AssetsDirectoryConfiguration : IEntityTypeConfiguration<AssetsDirectory>
{
	public void Configure(EntityTypeBuilder<AssetsDirectory> builder)
	{
		builder.HasKey(ad => ad.Id);

		builder.Property(ad => ad.Id).ValueGeneratedOnAdd();

		builder.Property(ad => ad.Name).IsRequired().HasMaxLength(200);

		builder.Property(ad => ad.CreatedAt).IsRequired();
		builder.Property(ad => ad.LastModifiedAt).IsRequired(false);

		builder.HasOne<Project>()
			.WithMany()
			.HasPrincipalKey(ad => ad.Id)
			.HasForeignKey(ad => ad.ProjectId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany<AssetsDirectory>()
			.WithOne()
			.HasPrincipalKey(ad => ad.Id)
			.HasForeignKey(ad => ad.DirectoryId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}