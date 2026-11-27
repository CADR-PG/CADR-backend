using Shared.Services;
using Shared.ValueObjects.SnowflakeIds;

namespace Community.Tests.Infrastructure;

internal sealed class SequentialSnowflakeIdProvider(long start = 1_000) : ISnowflakeIdProvider
{
	private long _next = start;

	public T GenerateId<T>() where T : struct, ISnowflakeId<T> => new() { Value = Interlocked.Increment(ref _next) };
}