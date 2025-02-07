using System.ComponentModel.DataAnnotations;

namespace Profile.Domain.Entities
{
    public class PostViewers : IProfileEntity
    {
        [Key]
        public long Id { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? UserIpAddress { get; set; }
    }
}
