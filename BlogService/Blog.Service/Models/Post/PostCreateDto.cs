using Blog.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Blog.Service.Models.Post
{
    public class PostCreateDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string? Text { get; set; }
        public Guid? SubscriptionLevelId { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
        public IFormFile? Thumbnail { get; set; }
        public IFormFileCollection? Photos { get; set; }
        public bool IsPartial { get; set; }
        public PostVisibility Visibility { get; set; }
        public List<int> Categories { get; set; } = [];
    }
}
