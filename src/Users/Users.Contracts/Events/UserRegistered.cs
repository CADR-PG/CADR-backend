using Shared.ValueObjects;
using Wolverine.Attributes;

namespace Users.Contracts.Events;

/// <summary>
/// Published (through the transactional outbox) when a new user account is created -
/// by email/password registration or by the first Google/GitHub login.
/// Consumers fetch the user data with <see cref="IUsersProvider" />.
/// </summary>
/// <remarks>
/// <see cref="MessageIdentityAttribute" /> gives the event a stable name on the wire, independent of the CLR type name,
/// so the contract can be renamed or moved without breaking messages in flight.
/// An attribute, because Wolverine has no fluent API for message type aliases.
/// </remarks>
[MessageIdentity("users.user-registered")]
public sealed record UserRegistered(UserId UserId)
{
	public const string TopicName = "users.user-registered";
}