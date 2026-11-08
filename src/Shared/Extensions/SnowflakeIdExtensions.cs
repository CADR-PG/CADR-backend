using Shared.ValueObjects;
using Shared.ValueObjects.SnowflakeIds;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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

	public sealed class SnowflakeIdConverter<T>
		: JsonConverter<T>
		where T : struct, ISnowflakeId<T>
	{
		public override T Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			var value = reader.TokenType switch
			{
				JsonTokenType.String => long.Parse(reader.GetString()!),
				JsonTokenType.Number => reader.GetInt64(),
				_ => throw new JsonException()
			};

			return new T { Value = value };
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
		}
	}
}