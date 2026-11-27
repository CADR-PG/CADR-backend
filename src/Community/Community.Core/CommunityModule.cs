using Community.Core.Database;
using Community.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Endpoints.Requests;
using Shared.Modules;
using Shared.Services;
using Shared.Settings;
using System.Runtime.CompilerServices;
using Users.Contracts.Events;
using Wolverine;
using Wolverine.AzureServiceBus;

[assembly: InternalsVisibleTo("Community.Tests")]
namespace Community.Core;

public class CommunityModule : IModule
{
	public static string Name => "Community";

	private static readonly (string Feature, string Tag)[] EndpointSections =
	[
		("UserProfiles", $"{Name} - User profiles"),
		("UserSettings", $"{Name} - User settings"),
		("Friends", $"{Name} - Friends"),
		("FriendRequests", $"{Name} - Friend requests"),
		("UserBlocks", $"{Name} - Blocked users"),
		("Chats", $"{Name} - Chats"),
		("ChatParticipants", $"{Name} - Chat participants"),
		("ChatMessages", $"{Name} - Chat messages"),
	];

	public void Register(IHostApplicationBuilder builder)
	{
		var services = builder.Services;
		var configuration = builder.Configuration;

		AddDatabase(builder);

		services.AddTimeProvider();
		services.AddSnowflakeIdProvider(configuration);
		services.AddScoped<FriendshipsService>();

		services.RegisterHttpRequestHandlers<CommunityModule>();
		services.AddValidatorsFromAssemblyContaining<CommunityModule>(includeInternalTypes: true);
	}

	public void RegisterMessageBroker(IHostApplicationBuilder builder)
	{
		AddDatabase(builder);

		builder.Services.ConfigureWolverine(options =>
		{
			ConfigureMessageHandlers(options);
			options.ListenToAzureServiceBusSubscription(Name.ToLowerInvariant()).FromTopic(UserRegistered.TopicName);
		});
	}

	public void RegisterFunctions(IHostApplicationBuilder builder) => AddDatabase(builder);

	internal static void ConfigureMessageHandlers(WolverineOptions options)
	{
		options.Discovery.IncludeAssembly(typeof(CommunityModule).Assembly);

		// DbContextOptions are registered by an opaque factory, so generated handler code has to resolve the DbContext
		// from the scoped container. Wolverine allows it automatically only for DbContexts registered before UseWolverine().
		options.CodeGeneration.AlwaysUseServiceLocationFor<CommunityDbContext>();
	}

	private static void AddDatabase(IHostApplicationBuilder builder)
	{
		var postgreSqlSettings = builder.Configuration.GetSettings<PostgreSqlSettings>();
		builder.Services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(postgreSqlSettings.ConnectionString,
			x => x.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Name)));
	}

	public void MapEndpoints(IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup(string.Empty);

		var featuresNamespace = $"{typeof(CommunityModule).Namespace}.Features";
		var sectionNamespaces = EndpointSections
			.Select(section => $"{featuresNamespace}.{section.Feature}")
			.ToHashSet(StringComparer.Ordinal);

		foreach (var (feature, tag) in EndpointSections)
		{
			var sectionNamespace = $"{featuresNamespace}.{feature}";
			group.MapGroup(string.Empty)
				.WithTags(tag)
				.MapEndpoints<CommunityModule>(type => type.Namespace == sectionNamespace);
		}

		group.MapGroup(string.Empty)
			.WithTags(Name)
			.MapEndpoints<CommunityModule>(type => type.Namespace is null || !sectionNamespaces.Contains(type.Namespace));
	}

	public async ValueTask RunInDevelopmentMode(IServiceProvider services)
	{
		var dbContext = services.GetRequiredService<CommunityDbContext>();

		if (!dbContext.Database.GetMigrations().Any()) return;

		await dbContext.Database.MigrateAsync();
	}
}