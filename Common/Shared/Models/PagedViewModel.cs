using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class PagedListViewModel<T>
{
    [Required]
    public int TotalPageCount { get; init; }
    [Required]
    public int PageSize { get; init; }
    [Required]
    public IReadOnlyList<T> Items { get; init; }

    public PagedListViewModel(int totalPageCount, int pageSize, IReadOnlyList<T> items)
    {
        TotalPageCount = totalPageCount;
        PageSize = pageSize;
        Items = items;
    }
}

public static class PagedListViewModel
{
    public static PagedListViewModel<T> Create<T>(IEnumerable<T> items, int pageSize, int totalCount) => new((int)Math.Ceiling((double)totalCount / pageSize), pageSize, [.. items]);

    public static PagedListViewModel<T> ToPagedList<T>(this IEnumerable<T> items, int pageSize, int totalCount) => Create(items, pageSize, totalCount);
}
