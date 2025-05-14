namespace Shared.ValueObjects;

public sealed record UserId(Guid Value)
{
	public static implicit operator Guid(UserId self) => self.Value;
	public static implicit operator UserId(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}