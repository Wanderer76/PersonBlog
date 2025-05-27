namespace Blog.Domain.Services.Models
{
    public class PostCommonModel
    {
        public Guid Id { get; set;}
        public string? PreviewUrl { get; set;}
        public string? Description { get; set;}
        public string Title { get; set; }
    }
}
