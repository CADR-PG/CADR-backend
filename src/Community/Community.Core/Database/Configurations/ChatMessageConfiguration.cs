using Community.Core.Entities.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
	public const int MaxContentLength = 4000;

	public void Configure(EntityTypeBuilder<ChatMessage> builder)
	{
		builder.HasKey(message => message.Id);
		builder.Property(message => message.Id).ValueGeneratedNever();
		builder.Property(message => message.Content).HasMaxLength(MaxContentLength);

		builder.HasOne(message => message.Sender)
			.WithMany()
			.HasForeignKey(message => new { message.ChatId, message.SenderId })
			.HasPrincipalKey(participant => new { participant.ChatId, participant.UserId })
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(message => message.ReferencedMessage)
			.WithMany()
			.HasForeignKey(message => message.ReferencedMessageId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(message => new { message.ChatId, message.Id });

		builder.HasIndex(message => new { message.ChatId, message.SenderId, message.ClientMessageId })
			.IsUnique()
			.HasFilter($"\"{nameof(ChatMessage.ClientMessageId)}\" IS NOT NULL");
	}
}