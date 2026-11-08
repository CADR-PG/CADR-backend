using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shared.ValueObjects.SnowflakeIds;

public sealed class EfCoreSnowflakeIdConverter<T>() : ValueConverter<T, long>(
	convertToProviderExpression: id => id.Value,
	convertFromProviderExpression: value => new T { Value = value })
	where T : struct, ISnowflakeId<T>;
