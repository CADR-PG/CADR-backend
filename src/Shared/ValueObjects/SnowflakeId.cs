namespace Shared.ValueObjects;

public interface ISnowflakeId<T> where T : struct
{
	long Value { get; init; }
}