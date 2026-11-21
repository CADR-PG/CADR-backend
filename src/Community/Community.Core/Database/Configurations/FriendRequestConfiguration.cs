using Community.Core.Entities.Friendships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
	public void Configure(EntityTypeBuilder<FriendRequest> builder)
	{
		builder.HasKey(friendRequest => friendRequest.Id);

		builder.HasOne(friendRequest => friendRequest.SenderProfile)
			.WithMany()
			.HasForeignKey(friendRequest => friendRequest.SenderId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(friendRequest => friendRequest.ReceiverProfile)
			.WithMany()
			.HasForeignKey(friendRequest => friendRequest.ReceiverId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(friendRequest => new { friendRequest.SenderId, friendRequest.ReceiverId }).IsUnique();
		builder.HasIndex(friendRequest => friendRequest.ReceiverId);
	}
}