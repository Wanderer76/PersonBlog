using Microsoft.AspNetCore.Http;

namespace Blog.Contracts.Models.Blog;

public class BlogCreateRequest
{
    public string Title { get; }
    public string? Description { get; }
    public IFormFile? PhotoUrl { get; }

    public BlogCreateRequest(string title, string? description, IFormFile? photoUrl)
    {
        Title = title;
        Description = description;
        PhotoUrl = photoUrl;
    }
}
