using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities
{
    public class PostViewer : IProfileEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ProfileId { get; set; }
        public bool? IsLike { get; set; }
        public bool IsViewed { get; set; }
        public string UserIpAddress { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
