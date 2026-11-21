using Shared.ValueObjects.SnowflakeIds;

namespace Shared.Extensions;

public static class SnowflakeIdExtensions
{
	extension<T>(ISnowflakeId<T> t) where T : struct
	{
		public static bool operator <(ISnowflakeId<T> left, ISnowflakeId<T> right) => left.Value < right.Value;
		public static bool operator >(ISnowflakeId<T> left, ISnowflakeId<T> right) => left.Value > right.Value;
		public static bool operator <=(ISnowflakeId<T> left, ISnowflakeId<T> right) => left.Value <= right.Value;
		public static bool operator >=(ISnowflakeId<T> left, ISnowflakeId<T> right) => left.Value >= right.Value;
	}
}