using Blog.Domain.Entities;

namespace Blog.Domain.Services.Models
{
    public class SubscriptionLevelModel
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public string Title { get; set; }
        public Guid? NextLevelId { get; set; }
        public double Price { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public static class SubscriptionLevelModelExtensions
    {
        public static SubscriptionLevelModel ToLevelModel(this SubscriptionLevel subscriptionLevel)
        {
            return new SubscriptionLevelModel
            {
                Id = subscriptionLevel.Id,
                BlogId = subscriptionLevel.BlogId,
                Description = subscriptionLevel.Description,
                NextLevelId = subscriptionLevel.NextLevelId,
                Price = subscriptionLevel.Price,
                PhotoUrl = subscriptionLevel.ImageId,
                Title = subscriptionLevel.Title,
            };
        }
    }
}
