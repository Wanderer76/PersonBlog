using Shared;
using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class SubscriptionLevel : BaseEntity<Guid>, IProfileEntity
    {
        public Guid BlogId { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public string? ImageId { get; set; }
        public Guid? NextLevelId { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        [ForeignKey(nameof(NextLevelId))]
        public SubscriptionLevel? NextSubscriptionLevel { get; set; }

        private SubscriptionLevel() { }

        public SubscriptionLevel(Guid id, Guid blogId, string title, string? description, double price, string? imageId, Guid? nextLevelId)
        {
            var now = DateTimeService.Now();
            Id = id;
            CreatedAt = now;
            BlogId = blogId;
            Title = title;
            Description = description;
            Price = price;
            ImageId = imageId;
            NextLevelId = nextLevelId;
            UpdatedAt = now;
        }
    }
}
