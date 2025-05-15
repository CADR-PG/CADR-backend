using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.ValueObjects;
using Users.Core.Entities;

namespace Users.Core.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.HasKey(u => u.Id);
		builder.Property(x => x.Id)
			.HasConversion(x => x.Value, x => new UserId(x))
			.ValueGeneratedOnAdd();

		builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
		builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
		builder.Property(u => u.HashedPassword).IsRequired().HasMaxLength(1024);
		builder.Property(u => u.Email).IsRequired().HasMaxLength(320);

		builder.HasMany(x => x.RefreshTokens)
			.WithOne()
			.HasPrincipalKey(x => x.Id)
			.HasForeignKey(x => x.UserId)
			.IsRequired();
	}
}