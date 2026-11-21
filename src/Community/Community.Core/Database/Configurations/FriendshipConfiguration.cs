using Community.Core.Entities.Friendships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
	public void Configure(EntityTypeBuilder<Friendship> builder)
	{
		builder.HasKey(friendship => new { friendship.LowerUserId, friendship.HigherUserId });

		builder.HasOne(friendship => friendship.LowerUser)
			.WithMany()
			.HasForeignKey(friendship => friendship.LowerUserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(friendship => friendship.HigherUser)
			.WithMany()
			.HasForeignKey(friendship => friendship.HigherUserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(friendship => friendship.HigherUserId);
	}
}
