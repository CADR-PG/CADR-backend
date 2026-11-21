using Community.Core.Entities.UserProfiles;
using Community.Core.Entities.UserSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
	public void Configure(EntityTypeBuilder<UserSettings> builder)
	{
		builder.HasKey(settings => settings.UserId);
		builder.Property(settings => settings.UserId).ValueGeneratedNever();

		builder.HasOne<UserProfile>()
			.WithOne()
			.HasForeignKey<UserSettings>(settings => settings.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}