using Shared.ValueObjects;

namespace Shop.Core.Entities;

public class Review
{
	public Guid Id { get; init; }
	public string? Text { get; init; }
	public int Rating { get; init; }
	public UserId AuthorId { get; init; }
}