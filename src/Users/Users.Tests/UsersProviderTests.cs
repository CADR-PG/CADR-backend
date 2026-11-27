using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;
using Testcontainers.PostgreSql;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.Services;

namespace Users.Tests;

public sealed class UsersProviderTests : IAsyncLifetime, IDisposable
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

	private UsersDbContext _dbContext = null!;

	public async Task InitializeAsync()
	{
		await _postgres.StartAsync();

		var options = new DbContextOptionsBuilder<UsersDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
		_dbContext = new UsersDbContext(options);
		await _dbContext.Database.MigrateAsync();
	}

	public async Task DisposeAsync()
	{
		await _dbContext.DisposeAsync();
		await _postgres.DisposeAsync();
	}

	public void Dispose() => _dbContext.Dispose();

	[Fact]
	public async Task GetUserIdsPaginatesByIdWithoutGapsOrDuplicates()
	{
		var userIds = await AddUsers(7);
		var provider = new UsersProvider(_dbContext);
		var pages = new List<int>();
		var returnedIds = new List<UserId>();
		UserId? after = null;

		while (true)
		{
			var page = await provider.GetUserIds(after, limit: 3, CancellationToken.None);
			if (page.Count == 0) break;

			pages.Add(page.Count);
			returnedIds.AddRange(page);
			after = page[^1];
		}

		Assert.Equal([3, 3, 1], pages);
		Assert.Equal(userIds.OrderBy(userId => userId.Value), returnedIds);
	}

	[Fact]
	public async Task GetUsersReturnsOnlyRequestedExistingUsers()
	{
		var userIds = await AddUsers(3);
		var unknownUserId = new UserId(Guid.NewGuid());

		var users = await new UsersProvider(_dbContext).GetUsers([userIds[0], userIds[2], unknownUserId], CancellationToken.None);

		Assert.Equal(
			new[] { userIds[0], userIds[2] }.Select(userId => userId.Value).Order(),
			users.Select(user => user.Id).Order());
		Assert.All(users, user => Assert.Equal($"{user.Id}@example.com", user.Email));
	}

	[Fact]
	public async Task GetUsersWithoutIdsReturnsEmpty()
		=> Assert.Empty(await new UsersProvider(_dbContext).GetUsers([], CancellationToken.None));

	[Fact]
	public async Task GetUserIdsRejectsNonPositiveLimit()
		=> await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() => new UsersProvider(_dbContext).GetUserIds(after: null, limit: 0, CancellationToken.None));

	private async Task<List<UserId>> AddUsers(int count)
	{
		var userIds = Enumerable.Range(0, count).Select(_ => new UserId(Guid.NewGuid())).ToList();

		foreach (var userId in userIds)
		{
			_dbContext.Users.Add(new User
			{
				Id = userId,
				Email = $"{userId}@example.com",
				HashedPassword = "hash",
				LastLoggedInAt = DateTime.UtcNow
			});
		}

		await _dbContext.SaveChangesAsync();
		_dbContext.ChangeTracker.Clear();
		return userIds;
	}
}