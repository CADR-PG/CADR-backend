using Community.Core.Database;
using Community.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Users.Contracts;

namespace Community.Core.Functions;

/// <summary>
/// Daily (00:00 UTC) reconciliation of user profiles and settings with the Users module - runs in the Functions worker.
/// </summary>
/// <remarks>
/// Public only because the Functions worker generates the function executor in its own assembly.
/// </remarks>
public sealed class SynchronizeUserProfilesFunction(CommunityDbContext dbContext, IUsersProvider usersProvider, ILoggerFactory loggerFactory)
{
	public const string Name = "Community-SynchronizeUserProfiles";

	/// <summary>NCRONTAB (with seconds), evaluated in UTC unless <c>WEBSITE_TIME_ZONE</c> is set.</summary>
	public const string Schedule = "0 0 0 * * *";

	[Function(Name)]
	public Task Run([TimerTrigger(Schedule)] TimerInfo timer, CancellationToken cancellationToken)
		=> new UserProfilesSynchronizer(dbContext, usersProvider, loggerFactory.CreateLogger<UserProfilesSynchronizer>())
			.SynchronizeAll(cancellationToken);
}