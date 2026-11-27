using Community.Core.Database.Extensions;
using Community.Core.Entities.UserProfiles;
using Community.Core.ValueObjects;
using System.Globalization;

namespace Community.Tests.Unit;

public sealed class LikePatternTests
{
	[Theory]
	[InlineData("abc", "abc")]
	[InlineData("100%", "100\\%")]
	[InlineData("a_b", "a\\_b")]
	[InlineData("c:\\dir", "c:\\\\dir")]
	[InlineData("\\%", "\\\\\\%")]
	public void EscapeEscapesWildcardsAndEscapeCharacter(string input, string expected)
		=> Assert.Equal(expected, LikePattern.Escape(input));

	[Fact]
	public void ContainsTrimsAndWrapsPhrase()
		=> Assert.Equal("%a\\_b%", LikePattern.Contains("  a_b  "));

	[Fact]
	public void ContainsTruncatesPhraseToMaxLength()
	{
		var phrase = new string('x', LikePattern.MaxPhraseLength + 1);

		Assert.Equal($"%{new string('x', LikePattern.MaxPhraseLength)}%", LikePattern.Contains(phrase));
	}

	[Fact]
	public void ContainsKeepsPhraseOfExactlyMaxLength()
	{
		var phrase = new string('x', LikePattern.MaxPhraseLength);

		Assert.Equal($"%{phrase}%", LikePattern.Contains(phrase));
	}

	[Fact]
	public void ContainsTrimsBeforeTruncating()
	{
		var phrase = "   " + new string('x', LikePattern.MaxPhraseLength) + "   ";

		Assert.Equal($"%{new string('x', LikePattern.MaxPhraseLength)}%", LikePattern.Contains(phrase));
	}
}

public sealed class ChatMessageIdTests
{
	[Theory]
	[InlineData("1", 1L)]
	[InlineData("9223372036854775807", long.MaxValue)]
	public void TryParseAcceptsPositiveIntegers(string input, long expected)
	{
		Assert.True(ChatMessageId.TryParse(input, CultureInfo.InvariantCulture, out var id));
		Assert.Equal(expected, id.Value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("0")]
	[InlineData("-1")]
	[InlineData("+1")]
	[InlineData(" 1")]
	[InlineData("1.0")]
	[InlineData("abc")]
	[InlineData("9223372036854775808")]
	public void TryParseRejectsInvalidValues(string? input)
	{
		Assert.False(ChatMessageId.TryParse(input, CultureInfo.InvariantCulture, out var id));
		Assert.Equal(default, id);
	}

	[Fact]
	public void ParseThrowsFormatExceptionForInvalidValue()
		=> Assert.Throws<FormatException>(() => ChatMessageId.Parse("abc", CultureInfo.InvariantCulture));

	[Fact]
	public void ToStringRoundTripsThroughParse()
	{
		var id = new ChatMessageId(123456789012345);

		Assert.Equal("123456789012345", id.ToString());
		Assert.Equal(id, ChatMessageId.Parse(id.ToString(), CultureInfo.InvariantCulture));
	}

	[Fact]
	public void ComparisonOperatorsCompareValues()
	{
		var lower = new ChatMessageId(1);
		var higher = new ChatMessageId(2);

		Assert.True(lower < higher);
		Assert.True(higher > lower);
		Assert.True(lower <= new ChatMessageId(1));
		Assert.True(higher >= new ChatMessageId(2));
		Assert.False(higher < lower);
		Assert.False(lower > higher);
	}

	[Fact]
	public void ChatIdTryParseRejectsZero()
		=> Assert.False(ChatId.TryParse("0", CultureInfo.InvariantCulture, out _));
}

public sealed class UserProfileDayStartTests
{
	[Fact]
	public void GetDayStartReturnsUtcMidnight()
	{
		var now = new DateTimeOffset(2026, 3, 10, 15, 30, 0, TimeSpan.Zero);

		Assert.Equal(new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero), UserProfile.GetDayStart(now));
	}

	[Fact]
	public void GetDayStartUsesUtcDateForNonUtcOffset()
	{
		var now = new DateTimeOffset(2026, 3, 10, 1, 0, 0, TimeSpan.FromHours(2));

		Assert.Equal(new DateTimeOffset(2026, 3, 9, 0, 0, 0, TimeSpan.Zero), UserProfile.GetDayStart(now));
	}

	[Fact]
	public void GetDayStartOfMidnightIsSameInstant()
	{
		var midnight = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);

		Assert.Equal(midnight, UserProfile.GetDayStart(midnight));
	}
}