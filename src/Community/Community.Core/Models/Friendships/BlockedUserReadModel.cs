using Community.Core.Entities.UserBlocks;
using Community.Core.Models.Profiles;
using System.Linq.Expressions;

namespace Community.Core.Models.Friendships;

internal sealed record BlockedUserReadModel
{
	public required UserProfileSummaryReadModel Profile { get; init; }
	public required DateTimeOffset BlockedAt { get; init; }

	public static readonly Expression<Func<UserBlock, BlockedUserReadModel>> MappingExpression =
		friendRequest =>
			new BlockedUserReadModel
			{
				Profile = new UserProfileSummaryReadModel
				{
					UserId = friendRequest.BlockedId,
					Name = friendRequest.BlockedProfile!.Name
				},
				BlockedAt = friendRequest.BlockedAt
			};

	public static Func<UserBlock, BlockedUserReadModel> Map { get; } = MappingExpression.Compile();
}