using IdGen;
using Microsoft.Extensions.Options;
using Shared.Settings;
using Shared.ValueObjects.SnowflakeIds;

namespace Shared.Services;

public interface ISnowflakeIdProvider
{
	T GenerateId<T>() where T : struct, ISnowflakeId<T>;
}

internal sealed class SnowflakeIdProvider(IIdGenerator<long> idGenerator) : ISnowflakeIdProvider
{
	public T GenerateId<T>() where T : struct, ISnowflakeId<T> => new() { Value = idGenerator.CreateId() };

	public static IdGenerator CreateGenerator(IOptions<SnowflakeIdSettings> options, TimeProvider timeProvider)
	{
		var settings = options.Value;

		var generatorOptions = new IdGeneratorOptions(
			IdStructure.Default,
			new TimeProviderTimeSource(settings.Epoch, timeProvider),
			SequenceOverflowStrategy.SpinWait);

		return new IdGenerator(settings.GeneratorId, generatorOptions);
	}

	private sealed class TimeProviderTimeSource(DateTimeOffset epoch, TimeProvider timeProvider) : ITimeSource
	{
		public DateTimeOffset Epoch { get; } = epoch;
		public TimeSpan TickDuration { get; } = TimeSpan.FromMilliseconds(1);

		public long GetTicks() => (long)(timeProvider.GetUtcNow() - Epoch).TotalMilliseconds;
	}
}
