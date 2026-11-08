using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;

namespace Projects.Core.ValueObjects;

public readonly record struct TeamId(long Value) : ISnowflakeId<TeamId>
{
	public override string ToString() => Value.ToString();
}
