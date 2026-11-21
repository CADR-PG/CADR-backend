using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Shared.ValueObjects.SnowflakeIds;

public interface ISnowflakeId<T> where T : struct
{
	long Value { get; init; }
}

/// <summary>
/// Shared parsing logic for snowflake identifiers (used by route/query binding via <see cref="IParsable{TSelf}"/> and JSON converters).
/// </summary>
public static class SnowflakeId
{
	public static bool TryParse<T>([NotNullWhen(true)] string? value, out T result) where T : struct, ISnowflakeId<T>
	{
		if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
		{
			result = new T { Value = parsed };
			return true;
		}

		result = default;
		return false;
	}

	public static T Parse<T>(string value) where T : struct, ISnowflakeId<T>
		=> TryParse<T>(value, out var result)
			? result
			: throw new FormatException($"`{value}` is not a valid {typeof(T).Name}.");

	public static string Format<T>(T id) where T : struct, ISnowflakeId<T>
		=> id.Value.ToString(CultureInfo.InvariantCulture);
}
