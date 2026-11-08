using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;

namespace Shared.Services;

public interface ISnowflakeIdProvider
{
	T GenerateId<T>() where T : struct, ISnowflakeId<T>;
}

public class SnowflakeIdProvider : ISnowflakeIdProvider
{
	public T GenerateId<T>() where T : struct, ISnowflakeId<T>
	{
		var value = 1;
		return new T { Value = value };
	}
}
