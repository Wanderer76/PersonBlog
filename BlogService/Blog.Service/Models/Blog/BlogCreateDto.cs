using Microsoft.AspNetCore.Http;

namespace Blog.Service.Models.Blog
{
    public class BlogCreateDto
    {
        public string Title { get; }
        public string? Description { get; }
        public IFormFile? PhotoUrl { get; }

        public BlogCreateDto(string title, string? description, IFormFile? photoUrl)
        {
            Title = title;
            Description = description;
            PhotoUrl = photoUrl;
        }
    }
}
