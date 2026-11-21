using Community.Core.Entities.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
	public const int MaxNameLength = 100;
	public const int MaxNoteLength = 500;

	public void Configure(EntityTypeBuilder<UserProfile> builder)
	{
		builder.HasKey(profile => profile.UserId);
		builder.Property(profile => profile.UserId).ValueGeneratedNever();
		builder.Property(profile => profile.FirstName).HasMaxLength(MaxNameLength);
		builder.Property(profile => profile.LastName).HasMaxLength(MaxNameLength);
		builder.Property(profile => profile.Name).HasMaxLength(MaxNameLength);
		builder.Property(profile => profile.Note).HasMaxLength(MaxNoteLength);

		builder.HasMany(profile => profile.Edits)
			.WithOne()
			.HasForeignKey(edit => edit.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

internal sealed class UserProfileEditConfiguration : IEntityTypeConfiguration<UserProfileEdit>
{
	public void Configure(EntityTypeBuilder<UserProfileEdit> builder)
	{
		builder.HasKey(edit => edit.Id);
		builder.Property(edit => edit.Id).ValueGeneratedNever();
		builder.Property(edit => edit.Name).HasMaxLength(UserProfileConfiguration.MaxNameLength);
		builder.Property(edit => edit.Note).HasMaxLength(UserProfileConfiguration.MaxNoteLength);

		builder.HasIndex(edit => new { edit.UserId, edit.EditedAt });
	}
}
