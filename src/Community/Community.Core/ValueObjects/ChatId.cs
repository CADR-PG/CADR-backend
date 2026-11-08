using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;

namespace Community.Core.ValueObjects;

public readonly record struct ChatId(long Value) : ISnowflakeId<ChatId>
{
	public override string ToString() => Value.ToString();
}
