using Blog.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Blog.API.Models;

public class PostCreateRequest
{

    [Required]
    public required string Title { get; set; }
    public PostType Type { get; set; }
    public PostVisibility Visibility { get; set; }
    public TextPostCreateForm? TextPostData { get; set; }
    public VideoPostCreateForm? VideoPostData { get; set; }
}

public sealed class VideoPostCreateForm
{
    public IFormFile? Thumbnail { get; set; }
    public string? Description { get; set; }
    public List<int> Categories { get; set; } = [];
}

public sealed class TextPostCreateForm
{
    public string Text {  get; set; }
    public IFormFileCollection? Files { get; set; }
}
