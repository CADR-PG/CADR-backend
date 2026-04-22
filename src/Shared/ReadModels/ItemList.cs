namespace Shared.ReadModels;

public sealed class ItemList<T>
{
	public required IEnumerable<T> Items { get; init; }
}

public static class ItemList
{
	public static ItemList<T> From<T>(IEnumerable<T> items) => new() { Items = items };
}