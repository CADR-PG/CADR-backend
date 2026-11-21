using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.ValueObjects.SnowflakeIds;

/// <summary>
/// Serializes snowflake ids as JSON strings (64-bit integers are not safe in JavaScript).
/// Accepts both string and number tokens when reading.
/// </summary>
public sealed class SnowflakeIdJsonConverter<T> : JsonConverter<T> where T : struct, ISnowflakeId<T>
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String when SnowflakeId.TryParse<T>(reader.GetString(), out var fromString):
				return fromString;
			case JsonTokenType.Number when reader.TryGetInt64(out var number) && number > 0:
				return new T { Value = number };
			default:
				throw new JsonException($"Invalid {typeof(T).Name} value.");
		}
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		=> writer.WriteStringValue(SnowflakeId.Format(value));
}