namespace Blog.Service.Models.Blog
{
    public class BlogEditDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
    }
}
