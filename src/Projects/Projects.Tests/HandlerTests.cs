using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NSubstitute;
using Projects.Core.Database;
using Projects.Core.Entities;
using Projects.Core.Features;
using Projects.Core.Features.Assets.Files;
using Projects.Core.Features.Projects;
using Projects.Core.ReadModels;
using Projects.Core.Services;
using Shared.Endpoints.Requests;
using Shared.ValueObjects;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace Projects.Tests;

public sealed class HandlerTests : IAsyncLifetime, IDisposable
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
		.WithImage("postgres:15-alpine")
		.Build();

	private ProjectsDbContext _dbContext = null!;

	public async Task InitializeAsync()
	{
		await _postgres.StartAsync();

		var options = new DbContextOptionsBuilder<ProjectsDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
		_dbContext = new ProjectsDbContext(options);
		await _dbContext.Database.MigrateAsync();
	}

	public async Task DisposeAsync()
	{
		await _dbContext.DisposeAsync();
		await _postgres.DisposeAsync();
	}

	public void Dispose() => _dbContext.Dispose();

	[Fact]
	public async Task CreateProject()
	{
		// arrange
		var projectData = new AddProject.Data("TestProject", "TestDescription");
		var userId = Guid.NewGuid();

		// act
		var request = new AddProject(projectData, new CurrentUser(userId));
		var handler = new AddProjectHandler(_dbContext);
		var result = await handler.Handle(request, CancellationToken.None);

		// assert
		Assert.IsType<Ok<ProjectReadModel>>(result);
	}

	[Fact]
	public async Task CreateFile()
	{
		// arrange
		var blobServiceClientMock = Substitute.For<BlobServiceClient>();
		var blobContainerClientMock = Substitute.For<BlobContainerClient>();
		var blobClientMock = Substitute.For<BlobClient>();

		blobServiceClientMock
			.GetBlobContainerClient(Arg.Any<string>())
			.Returns(blobContainerClientMock);

		blobContainerClientMock
			.GetBlobClient(Arg.Any<string>())
			.Returns(blobClientMock);

		var userId = Guid.NewGuid();
		var project = new Project()
		{
			Id = Guid.NewGuid(),
			JsonDocument = "{}",
			LastUpdate = DateTime.UtcNow,
			UserId = userId,
			Description = "TestProject",
			Name = "TestProject"
		};
		_dbContext.Projects.Add(project);
		await _dbContext.SaveChangesAsync();
		var fileId = Guid.NewGuid();
		var directory = AssetsDirectory.CreateRoot(project.Id);
		_dbContext.AssetsDirectories.Add(directory);
		await _dbContext.SaveChangesAsync();
		var size = 40u;
		var fileData = new CreateFile.Data("TestFile", directory.Id, size);

		// act
		var request = new CreateFile(fileData, new CurrentUser(userId), project.Id);
		var handler = new CreateFileHandler(_dbContext, blobServiceClientMock);
		var result = await handler.Handle(request, CancellationToken.None);

		// assert

		Assert.IsType<Created<AssetsFileUploadReadModel>>(result);

	}
}