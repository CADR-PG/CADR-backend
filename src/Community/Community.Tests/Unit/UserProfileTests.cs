using Community.Core.Database.Configurations;
using Community.Core.Entities.UserProfiles;
using Shared.ValueObjects;

namespace Community.Tests.Unit;

public sealed class UserProfileTests
{
	[Theory]
	[InlineData("alice.smith@example.com", "alice.smith")]
	[InlineData("  bob@example.com ", "bob")]
	[InlineData("no-at-sign", "no-at-sign")]
	[InlineData("@example.com", "@example.com")]
	[InlineData("   ", "user")]
	public void DefaultNameIsEmailLocalPart(string email, string expected)
		=> Assert.Equal(expected, UserProfile.GetDefaultName(email));

	[Fact]
	public void DefaultNameIsTruncated()
		=> Assert.Equal(UserProfileConfiguration.MaxNameLength,
			UserProfile.GetDefaultName($"{new string('a', UserProfileConfiguration.MaxNameLength + 1)}@example.com").Length);

	[Fact]
	public void CreatedProfileHasNoFirstAndLastNameAndIsNotCustomized()
	{
		var profile = UserProfile.Create(new UserId(Guid.NewGuid()), "alice@example.com");

		Assert.Null(profile.FirstName);
		Assert.Null(profile.LastName);
		Assert.Equal("alice", profile.Name);
		Assert.False(profile.IsNameCustomized);
	}
}