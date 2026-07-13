using Shared.ValueObjects;

namespace Shop.Core.Entities;

public class Review
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid GameId { get; set; }
	public int Rating { get; set; }
	public string Content { get; set; } = "";
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? UpdatedAt { get; set; }

}