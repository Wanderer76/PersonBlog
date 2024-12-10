using Microsoft.AspNetCore.Http;
using Profile.Domain.Entities;

namespace Profile.Service.Models.Post
{
    public class PostCreateDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string? Text {  get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
    }
}
