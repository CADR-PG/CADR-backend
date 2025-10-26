using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shared.ValueObjects;

public readonly record struct UserId(Guid Value)
{
	public static implicit operator Guid(UserId self) => self.Value;
	public static implicit operator UserId(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}

[UsedImplicitly]
public sealed class UserIdConverter() : ValueConverter<UserId, Guid>(uid => uid.Value, guid => new UserId(guid));