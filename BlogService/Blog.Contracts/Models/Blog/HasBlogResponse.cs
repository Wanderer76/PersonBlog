namespace Blog.Contracts.Models.Blog;
public sealed class HasBlogResponse
{
    public bool HasBlog { get; set; }
    public Guid? BlogId {  get; set; }
}
