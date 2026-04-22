using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Core.Entities;

namespace Users.Core.Database.Configurations;

internal sealed class UserSafeAccessPointConfiguration : IEntityTypeConfiguration<UserSafeAccessPoint>
{
	public void Configure(EntityTypeBuilder<UserSafeAccessPoint> builder)
	{
		builder.HasKey(ull => ull.Id);
		builder.Property(ull => ull.Id).ValueGeneratedNever();

		builder.HasOne<User>()
			.WithMany(u => u.UserSafeAccessPoints)
			.HasPrincipalKey(u => u.Id)
			.HasForeignKey(ull => ull.UserId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(all => all.Name).HasMaxLength(150).IsRequired();
		builder.Property(all => all.CreatedAt).IsRequired();
		builder.Property(all => all.EditedAt).IsRequired(false);
		builder.Property(all => all.Latitude).IsRequired();
		builder.Property(all => all.Longitude).IsRequired();
	}
}