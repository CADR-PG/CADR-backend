using Community.Core.Database;
using Community.Core.Database.Configurations;
using Community.Core.Database.Extensions;
using Community.Core.Entities.UserProfiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;
using Shared.Endpoints.Requests;
using Shared.Endpoints.Results;
using Shared.Endpoints.Validation;
using Shared.ValueObjects;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Community.Core.Features.UserProfiles;

internal sealed class MyUserProfileReadModel
{
	public required UserId UserId { get; init; }

	public required string? FirstName { get; init; }

	public required string? LastName { get; init; }

	public required string Name { get; init; }
	public required string? Note { get; init; }
	public required EditLimitReadModel EditLimit { get; init; }

	internal sealed class EditLimitReadModel
	{
		public required int MaxEditsPerDay { get; init; }
		public required int RemainingEdits { get; init; }

		public required DateTimeOffset ResetsAt { get; init; }
	}

	public static MyUserProfileReadModel Map(UserProfile profile, int editsToday, DateTimeOffset now) => new()
	{
		UserId = profile.UserId,
		FirstName = profile.FirstName,
		LastName = profile.LastName,
		Name = profile.Name,
		Note = profile.Note,
		EditLimit = new EditLimitReadModel
		{
			MaxEditsPerDay = UserProfile.MaxEditsPerDay,
			RemainingEdits = Math.Max(UserProfile.MaxEditsPerDay - editsToday, 0),
			ResetsAt = UserProfile.GetDayStart(now).AddDays(1)
		}
	};
}

internal static class UserProfileEditsQueries
{
	public static Task<int> CountEditsToday(this CommunityDbContext dbContext, UserId userId, DateTimeOffset now, CancellationToken cancellationToken)
	{
		var dayStart = UserProfile.GetDayStart(now);

		return dbContext.UserProfileEdits
			.CountAsync(edit => edit.UserId == userId && edit.EditedAt >= dayStart, cancellationToken);
	}
}

internal record struct GetMyUserProfile(CurrentUser CurrentUser) : IHttpRequest;

internal record struct UpdateMyUserProfile(
	CurrentUser CurrentUser,
	UpdateMyUserProfile.Body Payload
) : IHttpRequest
{
	internal sealed record Body(
		[property: Required][property: MaxLength(UserProfileConfiguration.MaxNameLength)] string Name,
		[property: MaxLength(UserProfileConfiguration.MaxNoteLength)] string? Note
	);
}

internal sealed class GetMyUserProfileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapGet<GetMyUserProfile, GetMyUserProfileHandler>("user-profile")
		.Produces<MyUserProfileReadModel>()
		.RequireAuthorization()
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`UserProfile.NotFound`");
}

internal sealed class UpdateMyUserProfileEndpoint : IEndpoint
{
	public static void Register(IEndpointRouteBuilder endpoints) => endpoints
		.MapPut<UpdateMyUserProfile, UpdateMyUserProfileHandler>("user-profile")
		.AddDataAnnotationsValidation<UpdateMyUserProfile.Body>()
		.Produces<MyUserProfileReadModel>()
		.RequireAuthorization()
		.ProducesError(400, "`ValidationError`, `UserProfile.InvalidName`")
		.ProducesError(401, "`UnauthorizedError`")
		.ProducesError(404, "`UserProfile.NotFound`")
		.ProducesError(429, "`UserProfile.EditLimitReached`");
}

internal sealed class GetMyUserProfileHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<GetMyUserProfile>
{
	public async Task<IResult> Handle(GetMyUserProfile request, CancellationToken cancellationToken)
	{
		var currentUserId = request.CurrentUser.Id;

		var profile = await dbContext.UserProfiles
			.AsNoTracking()
			.FirstOrDefaultAsync(userProfile => userProfile.UserId == currentUserId, cancellationToken);

		if (profile is null)
			return UserProfileErrors.NotFound();

		var now = timeProvider.GetUtcNow();
		var editsToday = await dbContext.CountEditsToday(currentUserId, now, cancellationToken);

		return Results.Ok(MyUserProfileReadModel.Map(profile, editsToday, now));
	}
}

internal sealed class UpdateMyUserProfileHandler(
	CommunityDbContext dbContext,
	TimeProvider timeProvider
) : IHttpRequestHandler<UpdateMyUserProfile>
{
	public async Task<IResult> Handle(UpdateMyUserProfile request, CancellationToken cancellationToken)
	{
		var (currentUser, payload) = request;

		var name = payload.Name.Trim();
		if (name.Length == 0)
			return new ErrorResult("UserProfile.InvalidName",
				$"Name must be between 1 and {UserProfileConfiguration.MaxNameLength} characters.");

		var note = string.IsNullOrWhiteSpace(payload.Note) ? null : payload.Note.Trim();

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		await dbContext.LockUserProfileForUpdate(currentUser.Id, cancellationToken);

		var profile = await dbContext.UserProfiles
			.FirstOrDefaultAsync(userProfile => userProfile.UserId == currentUser.Id, cancellationToken);

		if (profile is null)
			return UserProfileErrors.NotFound();

		var now = timeProvider.GetUtcNow();
		var editsToday = await dbContext.CountEditsToday(currentUser.Id, now, cancellationToken);

		var isChanged = profile.Name != name || profile.Note != note;

		if (isChanged && editsToday >= UserProfile.MaxEditsPerDay)
		{
			var resetsAt = UserProfile.GetDayStart(now).AddDays(1);
			return new ErrorResult("UserProfile.EditLimitReached",
				$"Profile can be edited at most {UserProfile.MaxEditsPerDay} times a day.", 429)
			{
				Details = [new ErrorResultDetail("resetsAt", [resetsAt.ToString("O", CultureInfo.InvariantCulture)])]
			};
		}

		if (profile.Edit(name, note, timeProvider) is not null)
		{
			editsToday++;
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);

		return Results.Ok(MyUserProfileReadModel.Map(profile, editsToday, now));
	}
}

internal static class UserProfileErrors
{
	public static ErrorResult NotFound()
		=> new("UserProfile.NotFound", "User profile does not exist.", 404);
}