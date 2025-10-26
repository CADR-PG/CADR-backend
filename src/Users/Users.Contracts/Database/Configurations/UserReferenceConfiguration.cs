using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.ValueObjects;
using Users.Contracts.Database.References;

namespace Users.Contracts.Database.Configurations;

public class UserReferenceConfiguration : IEntityTypeConfiguration<UserReference>
{
	public void Configure(EntityTypeBuilder<UserReference> builder)
	{
		builder.Metadata.SetIsTableExcludedFromMigrations(true);
		builder.ToTable("Users", UsersConstants.DatabaseSchema);
		builder.HasKey(u => u.Id);
	}
}