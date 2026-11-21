using Community.Core.Entities.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
	public void Configure(EntityTypeBuilder<ChatParticipant> builder)
	{
		builder.HasKey(participant => new { participant.ChatId, participant.UserId });
		builder.Property(participant => participant.Name).HasMaxLength(ChatConfiguration.MaxNameLength);

		builder.HasOne(participant => participant.UserProfile)
			.WithMany()
			.HasForeignKey(participant => participant.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(participant => participant.UserId);
	}
}
