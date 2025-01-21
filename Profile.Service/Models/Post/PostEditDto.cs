using Microsoft.AspNetCore.Http;

namespace Profile.Service.Models.Post
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
            this.Id = id;
            this.UserId = userId;
            this.Description = description;
            this.Title = title;
            this.PreviewId = previewId;
        }
    }
}
