using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints.Requests;
using Shared.ValueObjects;
using Testcontainers.PostgreSql;
using Users.Core.Database;
using Users.Core.Entities;
using Users.Core.Features;
using Users.Core.ReadModels;
using Users.Core.Services;

namespace Users.Tests;

public sealed class HandlersTest : IAsyncLifetime, IDisposable
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
		.WithImage("postgres:15-alpine")
		.Build();

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
	public async Task ChangePassword()
	{
		// arrange
		const string currentPassword = "dupaDupa123";
		const string newPassword = "321Dupadupa";
		var userId = new UserId(Guid.Parse("CA867F47-DB24-4021-B41C-F02CB139AC53"));
		_dbContext.Add(new User
		{
			Id = userId,
			FirstName = "Binjamin",
			LastName = "Netanjachuj",
			Email = "adolf@hitler.pl",
			HashedPassword = HashingService.Hash(currentPassword),
			LastLoggedInAt = DateTime.UtcNow
		});
		await _dbContext.SaveChangesAsync();

		var request = new ChangePassword(
			new ChangePassword.Data(currentPassword, newPassword),
			new CurrentUser(userId));

		// act
		var handler = new ChangePasswordHandler(_dbContext);
		var result = await handler.Handle(request, CancellationToken.None);

		// assert
		Assert.True(result is Ok<UserReadModel>);
		var userWithChangedPassword = await _dbContext.Users.SingleAsync(x => x.Id == userId);
		Assert.True(HashingService.IsValid(newPassword, userWithChangedPassword.HashedPassword));
	}
}