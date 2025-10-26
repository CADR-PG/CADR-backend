using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Core.Entities;

namespace Users.Core.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> user)
	{
		user.HasKey(u => u.Id);

		user.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
		user.Property(u => u.LastName).IsRequired().HasMaxLength(100);
		user.Ignore(u => u.FullName);
		user.Property(u => u.HashedPassword).IsRequired().HasMaxLength(1024);
		user.Property(u => u.Email).IsRequired().HasMaxLength(320);

		user.ComplexProperty(x => x.EmailConfirmation, confirmation =>
		{
			confirmation.Property(x => x.IsConfirmed)
				.HasDefaultValue(false)
				.IsRequired();

			confirmation.Property(x => x.Code)
				.IsRequired(false);

			confirmation.Property(x => x.ExpiresAt)
				.IsRequired(false);

			confirmation.Property(x => x.SentAt)
				.IsRequired(false);
		});

		user.HasMany(x => x.RefreshTokens)
			.WithOne()
			.HasForeignKey(x => x.UserId)
			.IsRequired();
	}
}