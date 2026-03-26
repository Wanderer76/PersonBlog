using Blog.Contracts.Services;

namespace Blog.API.Models
{
    public class PostEditForm : VideoPostCreateRequest
    {
        public Guid Id { get; set; }
    }
}
