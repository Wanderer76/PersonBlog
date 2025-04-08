using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class PersonBlog : IProfileEntity
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? PhotoUrl { get; set; }
        public Guid UserId { get; set; }
        public int SubscriptionsCount { get; set; }

        public List<Subscriber> Subscriptions { get; set; }
    }
}
