using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Funds;
using Shop.Core.Entities.Users;

namespace Shop.Core.Database.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallets>
{
	public void Configure(EntityTypeBuilder<Wallets> builder)
	{
		builder.HasKey(wallet => wallet.Id);
		builder.Property(wallet => wallet.Id).ValueGeneratedOnAdd();

		builder.Property(wallet => wallet.UserId).IsRequired();
		builder.HasIndex(wallet => wallet.UserId).IsUnique();

		builder.HasOne<ShopUser>()
			.WithOne()
			.HasForeignKey<Wallets>(wallet => wallet.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Transactions entity is not wired up into the model yet.
		builder.Ignore(wallet => wallet.Transactions);
	}
}