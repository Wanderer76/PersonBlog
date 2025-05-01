using System.ComponentModel.DataAnnotations;

namespace Video.Domain.Entities
{
    public class PostViewer : IVideoViewEntity
    {
        [Key]
        public required Guid Id { get; set; }
        public required Guid PostId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ProfileId { get; set; }
        public bool? IsLike { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? UserIpAddress { get; set; }
    }
}
