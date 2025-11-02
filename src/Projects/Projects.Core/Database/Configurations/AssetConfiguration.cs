using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities;

namespace Projects.Core.Database.Configurations;

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
	public void Configure(EntityTypeBuilder<Asset> builder)
	{
		builder.HasKey(a => a.Id);
		builder.Property(a => a.Id).ValueGeneratedOnAdd();
		builder.Property(a => a.Name)
			.IsRequired()
			.HasMaxLength(200);
		builder.Property(a => a.Type).IsRequired();
		builder.Property(a => a.ParentId)
			.IsRequired()
			.HasDefaultValue(null);
		builder.Property(a => a.BlobName).IsRequired();

		builder.HasOne(x => x.Project)
			.WithMany()
			.HasPrincipalKey(x => x.Id)
			.HasForeignKey(x => x.ProjectId)
			.IsRequired();

		builder.HasDiscriminator(a => a.Type)
			.HasValue<AssetFolder>(AssetType.Folder)
			.HasValue<AssetFile>(AssetType.File);
	}
}