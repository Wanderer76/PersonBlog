namespace Blog.API.Models
{
    public class PostEditForm : PostCreateRequest
    {
        public Guid Id { get; set; }
    }
}
