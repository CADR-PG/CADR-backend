using Community.Core.Entities.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
	public const int MaxNameLength = 100;

	public void Configure(EntityTypeBuilder<Chat> builder)
	{
		builder.HasKey(chat => chat.Id);
		builder.Property(chat => chat.Id).ValueGeneratedNever();
		builder.Property(chat => chat.Name).HasMaxLength(MaxNameLength);

		builder.HasMany(chat => chat.Participants)
			.WithOne(participant => participant.Chat)
			.HasForeignKey(participant => participant.ChatId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(chat => chat.Messages)
			.WithOne(message => message.Chat)
			.HasForeignKey(message => message.ChatId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(chat => new { chat.DirectChatLowerUserId, chat.DirectChatHigherUserId })
			.IsUnique()
			.HasFilter($"\"{nameof(Chat.Type)}\" = {(int)ChatType.Direct}");
	}
}
