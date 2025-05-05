using API.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Database.Configurations;

internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedNever();
		builder.Property(x => x.HashedCode).IsRequired().HasMaxLength(1024);
		builder.Property(x => x.CreatedAt).IsRequired();
		builder.Property(x => x.ExpiresAt).IsRequired();
		builder.HasOne<User>()
			.WithMany(x => x.RefreshTokens)
			.HasPrincipalKey(x => x.Id)
			.HasForeignKey(y => y.UserId)
			.IsRequired();
	}
}