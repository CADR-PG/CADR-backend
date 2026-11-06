using Microsoft.EntityFrameworkCore;

namespace Shared.Services;

public sealed record Listed<T>(IReadOnlyList<T> Items) where T : class;

public static class Listed
{
	public static async Task<Listed<T>> Create<T>(IQueryable<T> source) where T : class
	{
		var items = await source.ToListAsync();
		return new Listed<T>(items);
	}

	public static Listed<T> From<T>(IEnumerable<T> items) where T : class => new([.. items]);
}