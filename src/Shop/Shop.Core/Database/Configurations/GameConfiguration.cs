using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Database.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
	public void Configure(EntityTypeBuilder<Game> builder)
	{
		builder.HasKey(game => game.Id);
		builder.Property(game => game.Id).ValueGeneratedOnAdd();

		builder.Property(game => game.Title).IsRequired();
		builder.Property(game => game.Description).IsRequired();
		builder.Property(game => game.Price).IsRequired();

		builder.Property(game => game.CreatedAt).IsRequired();
		builder.Property(game => game.UpdatedAt).IsRequired(false);

		builder.Property(game => game.IsPublished).IsRequired();
		builder.Property(game => game.AuthorId).IsRequired();

	}
}