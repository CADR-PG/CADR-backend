using Community.Core;
using Community.Core.Database;
using Community.Core.Handlers;
using Community.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shared.Endpoints;
using Shared.Modules;
using Shared.Services;
using Shared.ValueObjects;
using Users.Contracts;
using Users.Contracts.Events;

namespace Community.Tests.Infrastructure;

internal sealed class CommunityTestHost : IAsyncDisposable
{
	private readonly ServiceProvider _services;

	public CommunityTestHost(PostgresFixture postgres)
	{
		Clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

		var services = new ServiceCollection();
		services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(postgres.ConnectionString));
		services.AddSingleton<TimeProvider>(Clock);
		services.AddSingleton<ISnowflakeIdProvider>(new SequentialSnowflakeIdProvider(Random.Shared.NextInt64(1, long.MaxValue / 2)));
		services.AddScoped<FriendshipsService>();
		services.AddSingleton(Users);
		services.AddScoped<IUsersProvider, FakeUsersProvider>();
		services.RegisterHttpRequestHandlers<CommunityModule>();

		_services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
	}

	public FakeTimeProvider Clock { get; }

	/// <summary>Users returned by <see cref="IUsersProvider" />.</summary>
	public FakeUsers Users { get; } = new();

	/// <summary>
	/// Registers the user in the (faked) Users module and consumes <see cref="UserRegistered" /> - the same way a new account reaches the Community module.
	/// The profile name is derived from the email: <c>{name}.{suffix}</c>.
	/// </summary>
	public async Task<UserId> CreateUser(string name, string suffix = "tester")
	{
		var userId = new UserId(Guid.NewGuid());
		Users.Add(userId, $"{name}.{suffix}@test.local");
		await ConsumeUserRegistered(userId);
		return userId;
	}

	/// <summary>Runs <see cref="UserRegisteredHandler" /> and saves changes the same way Wolverine's EF Core transactional middleware does.</summary>
	public Task ConsumeUserRegistered(UserId userId)
		=> Run(async services =>
		{
			var dbContext = services.GetRequiredService<CommunityDbContext>();
			var usersProvider = services.GetRequiredService<IUsersProvider>();
			await UserRegisteredHandler.Handle(new UserRegistered(userId), usersProvider, dbContext, CancellationToken.None);
			await dbContext.SaveChangesAsync();
		});

	public async Task<IResult> Send<TRequest>(TRequest request) where TRequest : IHttpRequest
	{
		await using var scope = _services.CreateAsyncScope();
		var handler = scope.ServiceProvider.GetRequiredService<IHttpRequestHandler<TRequest>>();
		return await handler.Handle(request, CancellationToken.None);
	}

	public async Task<T> Query<T>(Func<CommunityDbContext, Task<T>> query)
	{
		await using var scope = _services.CreateAsyncScope();
		return await query(scope.ServiceProvider.GetRequiredService<CommunityDbContext>());
	}

	public async Task<T> Run<T>(Func<IServiceProvider, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider);
	}

	public async Task Run(Func<IServiceProvider, Task> action)
	{
		await using var scope = _services.CreateAsyncScope();
		await action(scope.ServiceProvider);
	}

	public ValueTask DisposeAsync() => _services.DisposeAsync();
}