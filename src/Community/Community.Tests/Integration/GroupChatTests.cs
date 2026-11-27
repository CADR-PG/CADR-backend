using Community.Core.Entities.Chats;
using Community.Core.Entities.UserSettings;
using Community.Core.Features.ChatMessages;
using Community.Core.Features.ChatParticipants;
using Community.Core.Features.Chats;
using Community.Core.Features.UserSettings;
using Community.Core.Models.Chats;
using Community.Core.ValueObjects;
using Community.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Services;
using Shared.ValueObjects;

namespace Community.Tests.Integration;

public sealed class GroupChatTests(PostgresFixture postgres) : IntegrationTest(postgres)
{
	[Fact]
	public async Task CreatingGroupMakesOwnerAdminAndOthersMembers()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");

		var chat = ResultAssert.Created<ChatReadModel>(
			await Host.Send(new CreateGroupChat(As(owner), new CreateGroupChat.Body("  Team ", [member, member, owner]))));

		Assert.Equal("Team", chat.Name);
		Assert.Equal(ChatType.Group, chat.Type);
		Assert.Equal(new Dictionary<Guid, ChatParticipantRole?> { [owner.Value] = ChatParticipantRole.Admin, [member.Value] = ChatParticipantRole.Member },
			await RolesIn(owner, chat.Id));
	}

	[Fact]
	public async Task CreatingGroupWithWhitespaceNameIsRejected()
	{
		var owner = await Host.CreateUser("Owner");

		ResultAssert.Error(await Host.Send(new CreateGroupChat(As(owner), new CreateGroupChat.Body("   ", null))), "GroupChat.InvalidName", 400);
	}

	[Fact]
	public async Task CreatingGroupOverMemberLimitIsRejected()
	{
		var owner = await Host.CreateUser("Owner");
		var members = Enumerable.Range(0, CreateGroupChatHandler.MaxMembers).Select(_ => new UserId(Guid.NewGuid())).ToList();

		var result = await Host.Send(new CreateGroupChat(As(owner), new CreateGroupChat.Body("Big", members)));

		ResultAssert.Error(result, "GroupChat.TooManyMembers", 400);
	}

	[Theory]
	[InlineData(GroupInvitePolicy.Nobody, false, false)]
	[InlineData(GroupInvitePolicy.Nobody, true, false)]
	[InlineData(GroupInvitePolicy.FriendsOnly, false, false)]
	[InlineData(GroupInvitePolicy.FriendsOnly, true, true)]
	[InlineData(GroupInvitePolicy.Everyone, false, true)]
	public async Task AddingMemberRespectsInvitePolicy(GroupInvitePolicy policy, bool areFriends, bool expectedToBeAdded)
	{
		var owner = await Host.CreateUser("Owner");
		var invitee = await Host.CreateUser("Invitee");
		if (areFriends) await MakeFriends(owner, invitee);
		ResultAssert.Ok<UserSettingsReadModel>(await Host.Send(new UpdateUserSettings(As(invitee), new UpdateUserSettings.Body(policy))));
		var chatId = await CreateGroup(owner);

		var members = ResultAssert.Ok<Listed<ChatParticipantReadModel>>(
			await Host.Send(new AddGroupChatMembers(As(owner), chatId, new AddGroupChatMembers.Body([invitee]))));

		Assert.Equal(expectedToBeAdded, members.Items.Any(m => m.Profile.UserId == invitee.Value));
	}

	[Fact]
	public async Task CreatingGroupSkipsUsersWhoDoNotAllowInvites()
	{
		var owner = await Host.CreateUser("Owner");
		var shy = await Host.CreateUser("Shy");
		ResultAssert.Ok<UserSettingsReadModel>(await Host.Send(new UpdateUserSettings(As(shy), new UpdateUserSettings.Body(GroupInvitePolicy.Nobody))));

		var chatId = await CreateGroup(owner, shy);

		Assert.Equal([owner.Value], (await RolesIn(owner, chatId)).Keys);
	}

	[Fact]
	public async Task BlockedUsersCannotBeAddedInEitherDirection()
	{
		var owner = await Host.CreateUser("Owner");
		var blockedByOwner = await Host.CreateUser("BlockedByOwner");
		var blockingOwner = await Host.CreateUser("BlockingOwner");
		await Block(owner, blockedByOwner);
		await Block(blockingOwner, owner);
		var chatId = await CreateGroup(owner);

		var members = ResultAssert.Ok<Listed<ChatParticipantReadModel>>(
			await Host.Send(new AddGroupChatMembers(As(owner), chatId, new AddGroupChatMembers.Body([blockedByOwner, blockingOwner]))));

		Assert.Equal([owner.Value], members.Items.Select(m => m.Profile.UserId));
	}

	[Fact]
	public async Task OnlyAdminCanAddMembers()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var newcomer = await Host.CreateUser("Newcomer");
		var chatId = await CreateGroup(owner, member);

		var result = await Host.Send(new AddGroupChatMembers(As(member), chatId, new AddGroupChatMembers.Body([newcomer])));

		ResultAssert.Error(result, "GroupChat.Forbidden", 403);
	}

	[Fact]
	public async Task AddingMembersOverLimitIsRejected()
	{
		var owner = await Host.CreateUser("Owner");
		var members = new List<UserId>();
		for (var i = 0; i < AddGroupChatMembersHandler.MaxMembers - 1; i++)
			members.Add(await Host.CreateUser($"Member{i}"));
		var chatId = await CreateGroup(owner, [.. members]);
		var oneTooMany = await Host.CreateUser("OneTooMany");

		var result = await Host.Send(new AddGroupChatMembers(As(owner), chatId, new AddGroupChatMembers.Body([oneTooMany])));

		ResultAssert.Error(result, "GroupChat.TooManyMembers", 400);
	}

	[Fact]
	public async Task RemovedMemberIsReactivatedWhenAddedAgain()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);
		ResultAssert.NoContent(await Host.Send(new RemoveGroupChatMember(As(owner), chatId, member)));
		ResultAssert.Error(
			await Host.Send(new SendChatMessage(As(member), chatId, new SendChatMessage.Body(Guid.NewGuid(), "hi", null))),
			"Chat.NotFound", 404);
		Host.Clock.Advance(TimeSpan.FromHours(1));

		ResultAssert.Ok<Listed<ChatParticipantReadModel>>(
			await Host.Send(new AddGroupChatMembers(As(owner), chatId, new AddGroupChatMembers.Body([member]))));

		Assert.Equal(1, await Host.Query(db => db.ChatParticipants.CountAsync(p => p.ChatId == chatId && p.UserId == member)));
		var participant = await Host.Query(db => db.ChatParticipants.AsNoTracking().SingleAsync(p => p.ChatId == chatId && p.UserId == member));
		Assert.Null(participant.DeactivatedAt);
		Assert.Equal(Host.Clock.GetUtcNow(), participant.JoinedAt);
		await SendMessage(member, chatId);
	}

	[Fact]
	public async Task MemberCannotRemoveOthers()
	{
		var owner = await Host.CreateUser("Owner");
		var first = await Host.CreateUser("First");
		var second = await Host.CreateUser("Second");
		var chatId = await CreateGroup(owner, first, second);

		ResultAssert.Error(await Host.Send(new RemoveGroupChatMember(As(first), chatId, second)), "GroupChat.Forbidden", 403);
	}

	[Fact]
	public async Task AdminCannotBeRemovedWithoutDemotion()
	{
		var owner = await Host.CreateUser("Owner");
		var coAdmin = await Host.CreateUser("CoAdmin");
		var chatId = await CreateGroup(owner, coAdmin);
		ResultAssert.NoContent(await Host.Send(new PromoteGroupChatMember(As(owner), chatId, coAdmin)));

		ResultAssert.Error(await Host.Send(new RemoveGroupChatMember(As(owner), chatId, coAdmin)), "GroupChat.CannotRemoveAdmin", 403);
	}

	[Fact]
	public async Task RemovingSelfIsRejected()
	{
		var owner = await Host.CreateUser("Owner");
		var chatId = await CreateGroup(owner);

		ResultAssert.Error(await Host.Send(new RemoveGroupChatMember(As(owner), chatId, owner)), "GroupChat.CannotRemoveSelf", 400);
	}

	[Fact]
	public async Task LastAdminCannotBeDemoted()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);

		ResultAssert.Error(await Host.Send(new DemoteGroupChatMember(As(owner), chatId, owner)), "GroupChat.LastAdmin", 400);
		Assert.Equal(ChatParticipantRole.Admin, (await RolesIn(owner, chatId))[owner.Value]);
	}

	[Fact]
	public async Task AdminCanDemoteSelfWhenAnotherAdminExists()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);
		ResultAssert.NoContent(await Host.Send(new PromoteGroupChatMember(As(owner), chatId, member)));

		ResultAssert.NoContent(await Host.Send(new DemoteGroupChatMember(As(owner), chatId, owner)));

		var roles = await RolesIn(member, chatId);
		Assert.Equal(ChatParticipantRole.Member, roles[owner.Value]);
		Assert.Equal(ChatParticipantRole.Admin, roles[member.Value]);
	}

	[Fact]
	public async Task MemberCannotPromote()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);

		ResultAssert.Error(await Host.Send(new PromoteGroupChatMember(As(member), chatId, member)), "GroupChat.Forbidden", 403);
	}

	[Fact]
	public async Task LeavingLastAdminPromotesLongestMember()
	{
		var owner = await Host.CreateUser("Owner");
		var early = await Host.CreateUser("Early");
		var chatId = await CreateGroup(owner, early);
		Host.Clock.Advance(TimeSpan.FromMinutes(1));
		var late = await Host.CreateUser("Late");
		ResultAssert.Ok<Listed<ChatParticipantReadModel>>(
			await Host.Send(new AddGroupChatMembers(As(owner), chatId, new AddGroupChatMembers.Body([late]))));

		ResultAssert.NoContent(await Host.Send(new LeaveGroupChat(As(owner), chatId)));

		var roles = await RolesIn(early, chatId);
		Assert.False(roles.ContainsKey(owner.Value));
		Assert.Equal(ChatParticipantRole.Admin, roles[early.Value]);
		Assert.Equal(ChatParticipantRole.Member, roles[late.Value]);
	}

	[Fact]
	public async Task LastMemberLeavingDeletesGroup()
	{
		var owner = await Host.CreateUser("Owner");
		var chatId = await CreateGroup(owner);

		ResultAssert.NoContent(await Host.Send(new LeaveGroupChat(As(owner), chatId)));

		var chat = await Host.Query(db => db.Chats.AsNoTracking().SingleAsync(c => c.Id == chatId));
		Assert.NotNull(chat.DeletedAt);
		Assert.Equal(owner, chat.DeletedBy);
	}

	[Fact]
	public async Task LeavingDirectChatIsNotPossible()
	{
		var alice = await Host.CreateUser("Alice");
		var bob = await Host.CreateUser("Bob");
		await MakeFriends(alice, bob);
		var chatId = await StartDirectChat(alice, bob);

		ResultAssert.Error(await Host.Send(new LeaveGroupChat(As(alice), chatId)), "GroupChat.NotFound", 404);
	}

	[Fact]
	public async Task AnyMemberCanRenameGroup()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);

		ResultAssert.NoContent(await Host.Send(new RenameGroupChat(As(member), chatId, new RenameGroupChat.Body("  Renamed "))));

		Assert.Equal("Renamed", ResultAssert.Ok<ChatReadModel>(await Host.Send(new GetChatById(As(owner), chatId))).Name);
	}

	[Fact]
	public async Task AdminCanDeleteAnyMessageButMemberOnlyOwn()
	{
		var owner = await Host.CreateUser("Owner");
		var first = await Host.CreateUser("First");
		var second = await Host.CreateUser("Second");
		var chatId = await CreateGroup(owner, first, second);
		var firstMessage = await SendMessage(first, chatId);

		ResultAssert.Error(await Host.Send(new DeleteChatMessage(As(second), chatId, firstMessage.Id)), "ChatMessage.Forbidden", 403);
		ResultAssert.NoContent(await Host.Send(new DeleteChatMessage(As(owner), chatId, firstMessage.Id)));
	}

	[Fact]
	public async Task OnlyAdminCanPinInGroup()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);
		var message = await SendMessage(member, chatId);

		ResultAssert.Error(await Host.Send(new PinChatMessage(As(member), chatId, message.Id)), "PinChatMessage.Forbidden", 403);
		ResultAssert.NoContent(await Host.Send(new PinChatMessage(As(owner), chatId, message.Id)));
	}

	[Fact]
	public async Task OnlyAdminCanDeleteGroupAndDeletedGroupDisappears()
	{
		var owner = await Host.CreateUser("Owner");
		var member = await Host.CreateUser("Member");
		var chatId = await CreateGroup(owner, member);

		ResultAssert.Error(await Host.Send(new DeleteGroupChat(As(member), chatId)), "GroupChat.Forbidden", 403);
		ResultAssert.NoContent(await Host.Send(new DeleteGroupChat(As(owner), chatId)));

		ResultAssert.Error(await Host.Send(new GetChatById(As(member), chatId)), "Chat.NotFound", 404);
		var chats = ResultAssert.Ok<OffsetPaginated<ChatReadModel>>(await Host.Send(new GetPaginatedChats(As(member), null)));
		Assert.DoesNotContain(chats.Items, chat => chat.Id == chatId);
	}

	private async Task<Dictionary<Guid, ChatParticipantRole?>> RolesIn(UserId viewer, ChatId chatId)
	{
		var participants = ResultAssert.Ok<Listed<ChatParticipantReadModel>>(await Host.Send(new GetChatParticipants(As(viewer), chatId)));
		return participants.Items.ToDictionary(p => p.Profile.UserId, p => p.Role);
	}
}