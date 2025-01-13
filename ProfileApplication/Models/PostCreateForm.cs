using Profile.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProfileApplication.Models
{
    public class PostCreateForm
    {
        [Required]
        public required string Title { get; set; }
        public string? Text { get; set; }
        public PostType Type { get; set; }
        public IFormFile? Video { get; set; }
        public IFormFileCollection? Files { get; set; }
        public bool IsPartial {  get; set; }
    }
}
