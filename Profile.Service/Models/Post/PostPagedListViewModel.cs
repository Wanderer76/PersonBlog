namespace Blog.Service.Models.Post
{
    public class PostPagedListViewModel
    {
        public required int TotalPageCount { get; init; }
        public required IEnumerable<PostModel> Posts { get; init; }
    }
}
