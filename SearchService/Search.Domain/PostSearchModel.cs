namespace Search.Domain
{
    public class PostModel
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ViewCount { get; set; }
    }

    public class PostIndex
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ViewCount { get; set; }
        public List<WordScore> Keywords { get; set; }

        public PostModel ToPostModel()
        {
            return new PostModel
            {
                Id = Id,
                BlogId = BlogId,
                CreatedAt = CreatedAt,
                Title = Title,
                Description = Description,
                ViewCount = ViewCount,
            };
        }
    }
    public class WordScore
    {
        public string Word { get; set; }
        public double Score { get; set; }
    }
}
