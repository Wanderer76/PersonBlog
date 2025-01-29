using System.ComponentModel.DataAnnotations;

namespace ProfileApplication.Models
{
    public class BlogCreateForm
    {
        [Required]
        public string Title {  get; set; }
        public string? Description { get; set; }
        public IFormFile? PhotoUrl {  get; set; }
    }
}
