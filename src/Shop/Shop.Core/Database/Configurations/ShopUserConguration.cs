using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shop.Core.Entities.Library;
using Shop.Core.Entities.Users;

namespace Shop.Core.Database.Configurations;

internal sealed class ShopUserConguration : IEntityTypeConfiguration<ShopUser>
{
	public void Configure(EntityTypeBuilder<ShopUser> builder)
	{
		builder.HasKey(user => user.Id);
		builder.Property(user => user.Id).ValueGeneratedOnAdd();

		builder.HasMany(user => user.GamesLibrary)
			.WithOne()
			.HasForeignKey(entry => entry.UserId);
	}
}