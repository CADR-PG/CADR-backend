using Shared.ValueObjects.SnowflakeIds;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Community.Core.ValueObjects;

[JsonConverter(typeof(SnowflakeIdJsonConverter<ChatMessageId>))]
public readonly record struct ChatMessageId(long Value) : ISnowflakeId<ChatMessageId>, IParsable<ChatMessageId>
{
	public override string ToString() => SnowflakeId.Format(this);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ChatMessageId result)
		=> SnowflakeId.TryParse(s, out result);

	public static ChatMessageId Parse(string s, IFormatProvider? provider) => SnowflakeId.Parse<ChatMessageId>(s);

	public static bool operator <(ChatMessageId left, ChatMessageId right) => left.Value < right.Value;
	public static bool operator >(ChatMessageId left, ChatMessageId right) => left.Value > right.Value;
	public static bool operator <=(ChatMessageId left, ChatMessageId right) => left.Value <= right.Value;
	public static bool operator >=(ChatMessageId left, ChatMessageId right) => left.Value >= right.Value;
}