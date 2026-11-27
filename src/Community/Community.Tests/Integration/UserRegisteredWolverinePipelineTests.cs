using Community.Core;
using Community.Core.Database;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Messaging;
using Shared.ValueObjects;
using Users.Contracts;
using Users.Contracts.Events;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.Util;

namespace Community.Tests.Integration;

/// <summary>
/// Runs the handler through Wolverine's generated pipeline (handler discovery, code generation,
/// EF Core transactional middleware) - without the Azure Service Bus transport.
/// </summary>
[Collection(PostgresCollectionDefinition.Name)]
public sealed class UserRegisteredWolverinePipelineTests(PostgresFixture postgres) : IAsyncLifetime
{
	private readonly FakeUsers _users = new();
	private IHost _host = null!;

	public async Task InitializeAsync()
	{
		// Same registration order as the message broker: Wolverine first, then module services.
		var builder = Host.CreateApplicationBuilder();
		builder.UseWolverine(options =>
		{
			options.PersistMessagesWithPostgresql(postgres.ConnectionString, "wolverine_tests");
			options.UseEntityFrameworkCoreTransactions();
			options.ApplyMessageHandlingPolicies();
		});
		builder.Services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(postgres.ConnectionString));

		// Same as UsersModule.RegisterMessageBroker: internal implementation + service location allowed.
		builder.Services.AddSingleton(_users);
		builder.Services.AddScoped<IUsersProvider, FakeUsersProvider>();
		builder.Services.ConfigureWolverine(options => options.CodeGeneration.AlwaysUseServiceLocationFor<IUsersProvider>());

		builder.Services.ConfigureWolverine(CommunityModule.ConfigureMessageHandlers);

		_host = builder.Build();
		await _host.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _host.StopAsync();
		_host.Dispose();
	}

	[Fact]
	public void UserRegisteredHasStableMessageTypeName()
		=> Assert.Equal("users.user-registered", typeof(UserRegistered).ToMessageTypeName());

	[Fact]
	public async Task HandlerChangesAreSavedByTransactionalMiddleware()
	{
		var userId = new UserId(Guid.NewGuid());
		_users.Add(userId, "alice.smith@test.local");

		await _host.Services.GetRequiredService<IMessageBus>().InvokeAsync(new UserRegistered(userId));

		await using var scope = _host.Services.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
		var profile = await dbContext.UserProfiles.SingleAsync(x => x.UserId == userId);
		Assert.Equal("alice.smith", profile.Name);
		Assert.True(await dbContext.UserSettings.AnyAsync(x => x.UserId == userId));
	}
}