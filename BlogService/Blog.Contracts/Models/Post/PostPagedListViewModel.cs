namespace Blog.Contracts.Models.Post
{
    public class PostPagedListViewModel
    {
        public required int TotalPageCount { get; init; }
        public required int TotalPostsCount { get; init; }
        public required IEnumerable<PostModel> Posts { get; init; }
    }
}
