namespace Users.Core.ValueObjects;

internal readonly record struct TokenId(Guid Value)
{
	public static implicit operator Guid(TokenId self) => self.Value;
	public static implicit operator TokenId(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}