using Community.Core.Entities.Chats;
using Community.Core.Models.Profiles;
using System.Linq.Expressions;

namespace Community.Core.Models.Chats;

internal sealed class ChatParticipantReadModel
{
	public required UserProfileSummaryReadModel Profile { get; init; }
	public required string? Name { get; init; }
	public required DateTimeOffset? JoinedAt { get; init; }
	public required ChatParticipantRole? Role { get; init; }

	public static Expression<Func<ChatParticipant, ChatParticipantReadModel>> MappingExpression => chatParticipant => new ChatParticipantReadModel
	{
		Profile = new UserProfileSummaryReadModel
		{
			UserId = chatParticipant.UserId,
			Name = chatParticipant.UserProfile!.Name
		},
		Name = chatParticipant.Name,
		JoinedAt = chatParticipant.JoinedAt,
		Role = chatParticipant.Role
	};
}