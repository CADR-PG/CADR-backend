using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities;

namespace Projects.Core.Database.Configurations;

internal sealed class AssetsFilesConfiguration : IEntityTypeConfiguration<AssetsFile>
{
	public void Configure(EntityTypeBuilder<AssetsFile> builder)
	{
		builder.HasKey(af => af.Id);

		builder.Property(af => af.Id).ValueGeneratedOnAdd();

		builder.Property(af => af.Name).IsRequired().HasMaxLength(200);
		builder.Property(af => af.SizeInBytes).IsRequired();

		builder.Property(af => af.CreatedAt).IsRequired();
		builder.Property(af => af.LastModifiedAt).IsRequired();

		builder.HasOne<AssetsDirectory>()
			.WithMany(ad => ad.Files)
			.HasPrincipalKey(af => af.Id)
			.HasForeignKey(af => af.DirectoryId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne<Project>()
			.WithMany()
			.HasPrincipalKey(af => af.Id)
			.HasForeignKey(af => af.ProjectId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}