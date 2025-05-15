using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Core.Entities;
using Users.Core.ValueObjects;

namespace Users.Core.Database.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.HasConversion(x => x.Value, x => new TokenId(x))
			.ValueGeneratedOnAdd();

		builder.Property(x => x.CreatedAt).IsRequired();
		builder.Property(x => x.ExpiresAt).IsRequired();
	}
}