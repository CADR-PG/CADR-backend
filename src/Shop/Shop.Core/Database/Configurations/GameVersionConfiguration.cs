using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Database.Configurations;

internal sealed class GameVersionConfiguration : IEntityTypeConfiguration<GameVersion>
{
	public void Configure(EntityTypeBuilder<GameVersion> builder)
	{
		builder.HasKey(version => version.Id);
		builder.Property(version => version.Id).ValueGeneratedOnAdd();

		builder.Property(version => version.ProjectId).IsRequired();
		builder.Property(version => version.Version).IsRequired();
		builder.Property(version => version.CreatedAt).IsRequired();

		builder.HasOne(version => version.Game)
			.WithMany(game => game.Versions)
			.HasForeignKey(version => version.GameId);
	}
}