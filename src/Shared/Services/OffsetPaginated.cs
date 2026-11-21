using Microsoft.EntityFrameworkCore;

namespace Shared.Services;

public record OffsetPaginated<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize) where T : class
{
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	public bool HasPreviousPage => PageNumber > 1;

	public bool HasNextPage => PageNumber < TotalPages;
}

public static class OffsetPaginated
{
	public const int DefaultPageSize = 50;
	public const int MaxPageSize = 100;

	public static async Task<OffsetPaginated<T>> Create<T>(IQueryable<T> source, int? pageNumber = 1, int? pageSize = DefaultPageSize, CancellationToken cancellationToken = default) where T : class
	{
		var safePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
		// upper bound prevents int overflow of the offset (negative OFFSET -> database error)
		var safePageNumber = Math.Clamp(pageNumber ?? 1, 1, int.MaxValue / safePageSize);

		var count = await source.CountAsync(cancellationToken);
		var items = await source.Skip((safePageNumber - 1) * safePageSize)
			.Take(safePageSize)
			.ToListAsync(cancellationToken);

		return new OffsetPaginated<T>(items, count, safePageNumber, safePageSize);
	}
}