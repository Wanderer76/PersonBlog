using Microsoft.AspNetCore.Http;

namespace Blog.Contracts.Models.Blog;

public class BlogCreateRequest
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public IFormFile? PhotoUrl { get; set; }
    public BlogCreateRequest()
    {

    }

    public BlogCreateRequest(string title, string? description, IFormFile? photoUrl)
    {
        Title = title;
        Description = description;
        PhotoUrl = photoUrl;
    }
}
