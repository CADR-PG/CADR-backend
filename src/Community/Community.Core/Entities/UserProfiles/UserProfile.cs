using Community.Core.Database.Configurations;
using Shared.ValueObjects;

namespace Community.Core.Entities.UserProfiles;

internal sealed class UserProfile
{
	public const int MaxEditsPerDay = 2;

	private UserProfile() { }

	public UserId UserId { get; private init; }

	public string? FirstName { get; private init; }
	public string? LastName { get; private init; }

	public string Name { get; private set; } = null!;
	public string? Note { get; private set; }

	public bool IsNameCustomized { get; private set; }

	public List<UserProfileEdit> Edits { get; private init; } = [];

	/// <summary>
	/// Creates a profile of a new user. The Users module does not collect names, so the initial display name
	/// is derived from the email (the part before <c>@</c>) - the user can change it later.
	/// </summary>
	public static UserProfile Create(UserId userId, string email) => new()
	{
		UserId = userId,
		Name = GetDefaultName(email),
		IsNameCustomized = false
	};

	public static string GetDefaultName(string email)
	{
		var trimmedEmail = email.Trim();
		var atIndex = trimmedEmail.IndexOf('@', StringComparison.Ordinal);
		var localPart = (atIndex > 0 ? trimmedEmail[..atIndex] : trimmedEmail).Trim();

		if (localPart.Length == 0) localPart = DefaultName;

		return localPart.Length <= UserProfileConfiguration.MaxNameLength
			? localPart
			: localPart[..UserProfileConfiguration.MaxNameLength];
	}

	private const string DefaultName = "user";

	public UserProfileEdit? Edit(string name, string? note, TimeProvider timeProvider)
	{
		if (name == Name && note == Note) return null;

		Name = name;
		Note = note;
		IsNameCustomized = true;

		var edit = new UserProfileEdit
		{
			Id = Guid.CreateVersion7(),
			UserId = UserId,
			EditedAt = timeProvider.GetUtcNow(),
			Name = name,
			Note = note
		};

		Edits.Add(edit);
		return edit;
	}

	public static DateTimeOffset GetDayStart(DateTimeOffset now)
		=> new(now.UtcDateTime.Date, TimeSpan.Zero);
}

internal sealed class UserProfileEdit
{
	public required Guid Id { get; init; }
	public required UserId UserId { get; init; }
	public required DateTimeOffset EditedAt { get; init; }

	public required string Name { get; init; }
	public required string? Note { get; init; }
}