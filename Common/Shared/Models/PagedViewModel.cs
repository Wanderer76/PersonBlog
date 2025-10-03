using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    [Obsolete("убрать потом")]
    public class PagedViewModel<T>
    {
        public int TotalPageCount { get; init; }
        public int TotalPostsCount { get; init; }
        public IReadOnlyList<T> Items { get; init; }

        public PagedViewModel()
        {

        }
        public PagedViewModel(int totalPageCount, int totalPostsCount, IReadOnlyList<T> items)
        {
            TotalPageCount = totalPageCount;
            TotalPostsCount = totalPostsCount;
            Items = items;
        }
    }

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
}
