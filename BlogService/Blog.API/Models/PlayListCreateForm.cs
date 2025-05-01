namespace Blog.API.Models
{
    public class PlayListCreateForm
    {
        public required string Title { get; set; }
        public IFormFile? Thumbnail { get; set; }
        public List<Guid> PostIds { get; set; } = [];
    }
}
