using Search.Domain.Models;

namespace Search.Domain.Entities
{
    public class PostIndex : ISearch
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
}
