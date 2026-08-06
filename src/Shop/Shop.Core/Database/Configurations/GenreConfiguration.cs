using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Catalog;

namespace Shop.Core.Database.Configurations;

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
	public void Configure(EntityTypeBuilder<Genre> builder)
	{
		builder.HasKey(genre => genre.Id);
		builder.Property(genre => genre.Id).ValueGeneratedOnAdd();

		builder.Property(genre => genre.Name).IsRequired();

		builder.HasMany(genre => genre.Games);
	}
}