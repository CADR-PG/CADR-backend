namespace Users.Core.Entities;

public class EmailConfirmation
{
	public bool IsConfirmed { get; set; }
	public int? Code { get; set; }
	public DateTime? SentAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
}