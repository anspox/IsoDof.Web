using Microsoft.EntityFrameworkCore;

namespace IsoDof.Web.Models;

/// <summary>
/// Basit sayfalama sonucu. Liste sayfalarında "sonraki/önceki" için gerekli meta bilgiyi taşır.
/// </summary>
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    public PagedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 15;

        var totalCount = await source.CountAsync();
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(items, totalCount, pageNumber, pageSize);
    }
}
