using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Core.Entities;

namespace Users.Core.Database.Configurations;

internal sealed class AuthenticationLocationLogConfiguration : IEntityTypeConfiguration<UserLocationLog>
{
	public void Configure(EntityTypeBuilder<UserLocationLog> builder)
	{
		builder.HasKey(ull => ull.Id);
		builder.Property(ull => ull.Id).ValueGeneratedNever();

		builder.HasOne<User>()
			.WithMany(u => u.UserLocationLogs)
			.HasPrincipalKey(u => u.Id)
			.HasForeignKey(ull => ull.UserId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(all => all.AuthenticationType).IsRequired();
		builder.Property(all => all.OccuredAt).IsRequired();
		builder.Property(all => all.IpAddress).HasMaxLength(39).IsRequired();
		builder.Property(all => all.Latitude).IsRequired();
		builder.Property(all => all.Longitude).IsRequired();
		builder.Property(all => all.Country).HasMaxLength(150).IsRequired();
		builder.Property(all => all.City).HasMaxLength(150).IsRequired();
	}
}