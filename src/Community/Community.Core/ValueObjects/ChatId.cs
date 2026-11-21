using Shared.ValueObjects.SnowflakeIds;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Community.Core.ValueObjects;

[JsonConverter(typeof(SnowflakeIdJsonConverter<ChatId>))]
public readonly record struct ChatId(long Value) : ISnowflakeId<ChatId>, IParsable<ChatId>
{
	public override string ToString() => SnowflakeId.Format(this);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ChatId result)
		=> SnowflakeId.TryParse(s, out result);

	public static ChatId Parse(string s, IFormatProvider? provider) => SnowflakeId.Parse<ChatId>(s);

	public static bool operator <(ChatId left, ChatId right) => left.Value < right.Value;
	public static bool operator >(ChatId left, ChatId right) => left.Value > right.Value;
	public static bool operator <=(ChatId left, ChatId right) => left.Value <= right.Value;
	public static bool operator >=(ChatId left, ChatId right) => left.Value >= right.Value;
}
