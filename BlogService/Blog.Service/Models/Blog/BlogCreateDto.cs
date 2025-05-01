using Microsoft.AspNetCore.Http;

namespace Blog.Service.Models.Blog
{
    public class BlogCreateDto
    {
        public Guid UserId { get; }
        public string Title { get; }
        public string? Description { get; }
        public IFormFile? PhotoUrl { get; }

        public BlogCreateDto(Guid userId, string title, string? description, IFormFile? photoUrl)
        {
            UserId = userId;
            Title = title;
            Description = description;
            PhotoUrl = photoUrl;
        }
    }
}
