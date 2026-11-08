namespace Shared.ValueObjects.SnowflakeIds;

public interface ISnowflakeId<T> where T : struct
{
	long Value { get; init; }
}