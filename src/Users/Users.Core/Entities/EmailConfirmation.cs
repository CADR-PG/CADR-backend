namespace Users.Core.Entities;

public class EmailConfirmation
{
	public bool IsConfirmed { get; set; }
	public string? Code { get; set; }
	public string? Email { get; set; }
	public DateTime? SentAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
}