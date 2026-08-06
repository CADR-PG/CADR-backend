using Shared.ValueObjects;

namespace Shop.Core.Entities;

public class Review
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid GameId { get; set; }
	public int Rating { get; set; }
	public string Content { get; set; } = "";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; set; }

}