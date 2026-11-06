using Microsoft.EntityFrameworkCore;

namespace Shared.Services;

public sealed record CursorPaginated<T>(IReadOnlyList<T> Items, bool HasMore) where T : class;

public static class CursorPaginated
{
	public static async Task<CursorPaginated<T>> Create<T>(IQueryable<T> source, int pageSize, bool reverse = false) where T : class
	{
		var items = await source
			.Take(pageSize + 1)
			.ToListAsync();

		var hasMore = items.Count > pageSize;
		if (hasMore) items.RemoveAt(items.Count - 1);

		if (reverse) items.Reverse();

		return new CursorPaginated<T>(items, hasMore);
	}
}