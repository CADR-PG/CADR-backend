using System.Collections.ObjectModel;

namespace Users.Core.Services;
using Microsoft.EntityFrameworkCore;

public record Paginated<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
	where T : class
{
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	public bool HasPreviousPage => PageNumber > 1;

	public bool HasNextPage => PageNumber < TotalPages;
}

public static class Paginated
{
	public static async Task<Paginated<T>> Create<T>(IQueryable<T> source, int pageNumber = 1, int pageSize = 50) where T : class
	{
		var count = await source.CountAsync();
		var items = await source.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return new Paginated<T>(items, count, pageNumber, pageSize);
	}
}