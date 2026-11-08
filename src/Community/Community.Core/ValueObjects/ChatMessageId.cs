using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;
using System.Globalization;

namespace Community.Core.ValueObjects;

public readonly record struct ChatMessageId(long Value) : ISnowflakeId<ChatMessageId>
{
	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}