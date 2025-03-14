using Blog.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Blog.API.Models
{
    public class PostCreateForm
    {
        [Required]
        public required string Title { get; set; }
        public string? Description { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
        public IFormFile? PreviewId { get; set; }
        public IFormFileCollection? Files { get; set; }
        public bool IsPartial { get; set; }
    }
}
