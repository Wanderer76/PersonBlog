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
}
