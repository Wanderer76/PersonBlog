using Profile.Domain.Entities;

namespace ProfileApplication.Models
{
    public class PostCreateForm
    {
        public string Title { get; set; }
        public string? Text { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
        public IFormFileCollection Files { get; set; }
    }
}
