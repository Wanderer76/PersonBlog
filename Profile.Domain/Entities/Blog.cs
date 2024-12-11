using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class Blog : IProfileEntity
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? PhotoUrl { get; set; }
        public Guid ProfileId { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public AppProfile Profile { get; set; }
        public List<Subscriptions> Subscriptions { get; set; }
    }
}
