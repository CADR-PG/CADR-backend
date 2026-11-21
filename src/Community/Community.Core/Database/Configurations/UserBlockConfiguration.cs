using Community.Core.Entities.UserBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.Core.Database.Configurations;

internal sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
	public void Configure(EntityTypeBuilder<UserBlock> builder)
	{
		builder.HasKey(userBlock => new { userBlock.BlockerId, userBlock.BlockedId });

		builder.HasOne(userBlock => userBlock.BlockerProfile)
			.WithMany()
			.HasForeignKey(userBlock => userBlock.BlockerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(userBlock => userBlock.BlockedProfile)
			.WithMany()
			.HasForeignKey(userBlock => userBlock.BlockedId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(userBlock => userBlock.BlockedId);
	}
}