using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Database.Configurations;

internal sealed class GameInsightsConfiguration : IEntityTypeConfiguration<GamePlays>
{
	public void Configure(EntityTypeBuilder<GamePlays> builder)
	{
		builder.HasKey(i => i.Id);
		builder.Property(i => i.UserId).IsRequired();
		builder.Property(i => i.GameId).IsRequired();
		builder.Property(i => i.FirstPlayedAt).IsRequired();
		builder.Property(i => i.LastPlayedAt).IsRequired();

		builder.HasOne<Game>().WithMany().HasForeignKey(i => i.GameId);
		builder.HasIndex(i => new { i.GameId, i.UserId }).IsUnique();
	}
}