using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Video.Domain.Entities
{
    //TODO На nosql бдшке
    public class Comment : IVideoEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid FromUser { get; set; }
        public Guid PostId { get; set; }
        public required string CommentText { get; set; }
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
        public Guid? CommentId { get; set; }

        [ForeignKey(nameof(CommentId))]
        public Comment? ParentComment { get; set; }
    }
}
