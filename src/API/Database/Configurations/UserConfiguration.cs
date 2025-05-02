using API.Modules.Users.Entities;
using API.Modules.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Database.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.HasKey(u => u.Id);

		builder.Property(x => x.Id).HasConversion(x => x.Value, x => new UserId(x)).ValueGeneratedNever();
		builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
		builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
		builder.Property(u => u.HashedPassword).IsRequired().HasMaxLength(1024);
		builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
	}
}