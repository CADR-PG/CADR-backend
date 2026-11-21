using Community.Core.Entities.UserProfiles;
using System.Linq.Expressions;

namespace Community.Core.Models.Profiles;

internal sealed class UserProfileSummaryReadModel
{
	public required Guid UserId { get; set; }
	public required string Name { get; set; }

	public static readonly Expression<Func<UserProfile, UserProfileSummaryReadModel>> MappingExpression = userProfile =>
		new UserProfileSummaryReadModel { UserId = userProfile.UserId, Name = userProfile.Name, };

	public static Func<UserProfile, UserProfileSummaryReadModel> Map { get; } = MappingExpression.Compile();
}