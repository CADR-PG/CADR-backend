using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Library;

namespace Shop.Core.Database.Configurations;

internal sealed class LibraryEntryConfiguration : IEntityTypeConfiguration<LibraryEntry>
{
	public void Configure(EntityTypeBuilder<LibraryEntry> builder)
	{
		builder.HasKey(entry => entry.Id);
		builder.Property(entry => entry.Id).ValueGeneratedOnAdd();

		builder.Property(entry => entry.UserId).IsRequired();
		builder.Property(entry => entry.AcquiredAt).IsRequired();

		builder.HasOne(entry => entry.Game)
			.WithMany()
			.HasForeignKey(entry => entry.GameId);

		builder.HasOne(entry => entry.Order)
			.WithMany()
			.HasForeignKey(entry => entry.OrderId);

		builder.HasIndex(entry => new { entry.UserId, entry.GameId }).IsUnique();
	}
}