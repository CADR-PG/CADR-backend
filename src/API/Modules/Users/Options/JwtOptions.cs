namespace API.Modules.Users.Options;

internal class JwtOptions
{
	public string? Issuer { get; set; }
	public string? Audience { get; set; }
	public string? Secret { get; set; }
	public int ExpireTimeInDays { get; set; }
}