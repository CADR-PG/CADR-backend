namespace API.Modules.Users.ValueObjects;

internal sealed record UserId(Guid Value)
{
	public static implicit operator Guid(UserId self) => self.Value;
	public static implicit operator UserId(Guid value) => new(value);
}