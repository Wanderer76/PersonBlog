using System.ComponentModel.DataAnnotations;

namespace Comments.Domain.Models
{
    public class CommentUpdateRequest
    {
        public Guid CommentId {  get; set; }

        [Required]
        public string Text {  get; set; }
    }
}
