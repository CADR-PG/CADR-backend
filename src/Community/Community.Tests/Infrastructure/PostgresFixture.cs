using Community.Core.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Community.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

	public string ConnectionString => _container.GetConnectionString();

	public async Task InitializeAsync()
	{
		await _container.StartAsync();

		var options = new DbContextOptionsBuilder<CommunityDbContext>().UseNpgsql(ConnectionString).Options;
		await using var dbContext = new CommunityDbContext(options);

		await dbContext.Database.MigrateAsync();

		// Minimal table of the Users module, read by Community through UserReference (excluded from Community migrations).
		await dbContext.Database.ExecuteSqlRawAsync("""
			CREATE SCHEMA IF NOT EXISTS "Users";
			CREATE TABLE IF NOT EXISTS "Users"."Users" ("Id" uuid PRIMARY KEY, "Email" varchar(320) NOT NULL);
			""");
	}

	public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>
{
	public const string Name = "Postgres";
}