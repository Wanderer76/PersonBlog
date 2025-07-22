using System.ComponentModel.DataAnnotations;

namespace Comments.Domain.Models
{
    public class CommentCreateRequest
    {
        public Guid PostId { get; set; }
        public Guid? ReplyTo {  get; set; }
        
        [Required]
        public string Text {  get; set; }
    }

    public class CommentCreateResponse
    {
        public Guid Id { get; set; }
        public Guid? ReplyTo { get; set; }

        [Required]
        public string Text { get; set; }
        public string Username {  get; set; }
        public Guid UserId { get; set; }
        public string PhotoUrl {  get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
