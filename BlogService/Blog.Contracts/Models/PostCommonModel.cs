namespace Blog.Contracts.Models;

public class PostCommonModel
{
    public Guid Id { get; set; }
    public string? PreviewObjectName { get; set; }
    public string? Description { get; set; }
    public string Title { get; set; }
}
