using Community.Core.Entities.Chats;
using Community.Core.Entities.Friendships;
using Community.Core.Entities.UserBlocks;
using Community.Core.Entities.UserProfiles;
using Community.Core.Entities.UserSettings;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;
using Users.Contracts.Database.References;

namespace Community.Core.Database;

internal sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
	public DbSet<Chat> Chats { get; init; } = null!;
	public DbSet<ChatParticipant> ChatParticipants { get; init; } = null!;
	public DbSet<ChatMessage> ChatMessages { get; init; } = null!;
	public DbSet<ChatMessagePin> ChatMessagePins { get; init; } = null!;
	public DbSet<UserProfile> UserProfiles { get; init; } = null!;
	public DbSet<UserProfileEdit> UserProfileEdits { get; init; } = null!;
	public DbSet<Friendship> Friendships { get; init; } = null!;
	public DbSet<FriendRequest> FriendRequests { get; init; } = null!;
	public DbSet<UserBlock> UserBlocks { get; init; } = null!;
	public DbSet<UserSettings> UserSettings { get; init; } = null!;

	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.HasDefaultSchema(CommunityModule.Name);
		builder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
		builder.ApplyConfigurationsFromAssembly(typeof(UserReference).Assembly);
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