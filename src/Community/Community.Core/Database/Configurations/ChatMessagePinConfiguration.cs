using Community.Core.Entities.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class ChatMessagePinConfiguration : IEntityTypeConfiguration<ChatMessagePin>
{
	public void Configure(EntityTypeBuilder<ChatMessagePin> builder)
	{
		builder.HasKey(pin => new { pin.ChatId, pin.MessageId });

		builder.HasOne(pin => pin.Message)
			.WithMany()
			.HasForeignKey(pin => pin.MessageId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}