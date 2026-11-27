using Community.Core.Entities.Chats;
using Community.Core.Entities.Friendships;
using Community.Core.Entities.UserBlocks;
using Community.Core.Entities.UserProfiles;
using Community.Core.Entities.UserSettings;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;

namespace Community.Core.Database;

/// <remarks>
/// Public only because Wolverine generates handler code in a separate assembly and has to reference it.
/// Entity sets stay internal to the module.
/// </remarks>
public sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	internal DbSet<Chat> Chats => Set<Chat>();
	internal DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
	internal DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
	internal DbSet<ChatMessagePin> ChatMessagePins => Set<ChatMessagePin>();
	internal DbSet<UserProfile> UserProfiles => Set<UserProfile>();
	internal DbSet<UserProfileEdit> UserProfileEdits => Set<UserProfileEdit>();
	internal DbSet<Friendship> Friendships => Set<Friendship>();
	internal DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
	internal DbSet<UserBlock> UserBlocks => Set<UserBlock>();
	internal DbSet<UserSettings> UserSettings => Set<UserSettings>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(CommunityModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
		base.OnModelCreating(builder);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder
			.Properties<UserId>()
			.HaveConversion<UserIdConverter>();

		configurationBuilder.RegisterSnowflakeIdsConversion<CommunityModule>();
	}
}