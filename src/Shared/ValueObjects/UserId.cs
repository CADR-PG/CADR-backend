using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.ValueObjects;

[JsonConverter(typeof(UserIdJsonConverter))]
public readonly record struct UserId(Guid Value) : IParsable<UserId>
{
	public static implicit operator Guid(UserId self) => self.Value;
	public static implicit operator UserId(Guid value) => new(value);

	public override string ToString() => Value.ToString();

	public static OrderedUserIdPair GetLowerAndHigherUserId(UserId firstUserId, UserId secondUserId)
	{
		return firstUserId.Value.CompareTo(secondUserId) < 0
			? new OrderedUserIdPair(firstUserId, secondUserId)
			: new OrderedUserIdPair(secondUserId, firstUserId);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UserId result)
	{
		if (Guid.TryParse(s, provider, out var guid) && guid != Guid.Empty)
		{
			result = new UserId(guid);
			return true;
		}

		result = default;
		return false;
	}

	public static UserId Parse(string s, IFormatProvider? provider)
		=> TryParse(s, provider, out var result)
			? result
			: throw new FormatException($"`{s}` is not a valid {nameof(UserId)}.");
}

public sealed record OrderedUserIdPair(UserId LowerUserId, UserId UpperUserId);

[UsedImplicitly]
public sealed class UserIdConverter() : ValueConverter<UserId, Guid>(uid => uid.Value, guid => new UserId(guid));

public sealed class UserIdJsonConverter : JsonConverter<UserId>
{
	public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out var guid) && guid != Guid.Empty
			? new UserId(guid)
			: throw new JsonException($"Invalid {nameof(UserId)} value.");

	public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.Value);
}
