namespace API.Modules.Users.Options;

internal class RefreshTokenOptions
{
	public string? Secret { get; set; }
	public int ExpireTimeInDays { get; set; }
}