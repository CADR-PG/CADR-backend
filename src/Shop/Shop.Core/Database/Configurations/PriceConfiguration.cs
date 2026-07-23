using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Funds;

namespace Shop.Core.Database.Configurations;

internal sealed class PriceConfiguration : IEntityTypeConfiguration<Price>
{
	public void Configure(EntityTypeBuilder<Price> builder)
	{
		builder.HasKey(price => price.GameId);

		builder.HasOne(price => price.Game)
			.WithOne(game => game.Price)
			.HasForeignKey<Price>(price => price.GameId);

		builder.Property(price => price.Amount).IsRequired();
		builder.Property(price => price.Currency).IsRequired().HasMaxLength(3);
		builder.Property(price => price.ValidFrom).IsRequired();
	}
}