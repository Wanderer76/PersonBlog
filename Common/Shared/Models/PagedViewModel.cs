namespace Shared.Models
{
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
}
