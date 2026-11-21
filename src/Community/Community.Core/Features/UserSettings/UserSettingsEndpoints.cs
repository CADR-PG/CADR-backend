using Community.Core.Database;
using Community.Core.Database.Extensions;
using Community.Core.Entities.UserSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using UserSettingsEntity = Community.Core.Entities.UserSettings.UserSettings;

namespace Community.Core.Features.UserSettings;

internal sealed class UserSettingsReadModel
{
	public required GroupInvitePolicy GroupInvitePolicy { get; init; }

	public static UserSettingsReadModel Default { get; } = new() { GroupInvitePolicy = GroupInvitePolicy.Everyone };

	public static readonly Expression<Func<UserSettingsEntity, UserSettingsReadModel>> MappingExpression =
		settings => new UserSettingsReadModel { GroupInvitePolicy = settings.GroupInvitePolicy };
}

internal record struct GetUserSettings(CurrentUser CurrentUser) : IHttpRequest;

internal record struct UpdateUserSettings(
	CurrentUser CurrentUser,
	UpdateUserSettings.Body Payload
) : IHttpRequest
{
	internal sealed record Body([property: Required][property: EnumDataType(typeof(GroupInvitePolicy))] GroupInvitePolicy? GroupInvitePolicy);
}

internal sealed class GetUserSettingsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetUserSettings, GetUserSettingsHandler>("user-settings")
		.Produces<UserSettingsReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class UpdateUserSettingsEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPut<UpdateUserSettings, UpdateUserSettingsHandler>("user-settings")
		.AddDataAnnotationsValidation<UpdateUserSettings.Body>()
		.Produces<UserSettingsReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`")
		.ProducesError(401, "`UnauthorizedError`");
}

internal sealed class GetUserSettingsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<GetUserSettings>
{
	public async Task<IResult> Handle(GetUserSettings request, CancellationToken cancellationToken)
	{
		var readModel = await dbContext.UserSettings
			.AsNoTracking()
			.Where(settings => settings.UserId == request.CurrentUser.Id)
			.Select(UserSettingsReadModel.MappingExpression)
			.FirstOrDefaultAsync(cancellationToken);

		return Results.Ok(readModel ?? UserSettingsReadModel.Default);
	}
}

internal sealed class UpdateUserSettingsHandler(
	CommunityDbContext dbContext
) : IHttpRequestHandler<UpdateUserSettings>
{
	public async Task<IResult> Handle(UpdateUserSettings request, CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var settings = await dbContext.UserSettings
			.FirstOrDefaultAsync(x => x.UserId == currentUser.Id, cancellationToken);

		if (settings is null)
		{
			settings = new UserSettingsEntity { UserId = currentUser.Id };
			dbContext.UserSettings.Add(settings);
		}

		settings.GroupInvitePolicy = payload.GroupInvitePolicy!.Value;

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (exception.IsUniqueViolation())
		{
			dbContext.ChangeTracker.Clear();
			await dbContext.UserSettings
				.Where(x => x.UserId == currentUser.Id)
				.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.GroupInvitePolicy, payload.GroupInvitePolicy!.Value), cancellationToken);
		}

		return Results.Ok(new UserSettingsReadModel { GroupInvitePolicy = payload.GroupInvitePolicy!.Value });
	}
}