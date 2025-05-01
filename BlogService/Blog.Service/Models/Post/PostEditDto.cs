using Microsoft.AspNetCore.Http;

namespace Blog.Service.Models.Post
{
    public class PostEditDto
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public string? Description { get; }
        public string Title { get; }
        public IFormFile? PreviewId { get; }

        public PostEditDto(Guid id, Guid userId, string? description, string title, IFormFile? previewId)
        {
            Id = id;
            UserId = userId;
            Description = description;
            Title = title;
            PreviewId = previewId;
        }
    }
}
