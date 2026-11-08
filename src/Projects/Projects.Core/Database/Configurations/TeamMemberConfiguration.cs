using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Core.Entities.Teams;

namespace Projects.Core.Database.Configurations;

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
	public void Configure(EntityTypeBuilder<TeamMember> builder)
	{
		builder.HasKey(tm => new { tm.UserId, tm.TeamId });

		builder.Property(tm => tm.Role).IsRequired();
		builder.Property(tm => tm.JoinedAt).IsRequired();
		builder.Property(tm => tm.LeftAt).IsRequired(false);
	}
}
