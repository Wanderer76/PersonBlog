using Blog.Domain.Services.Models;

namespace Blog.Domain.Services
{
    public interface ISubscriptionLevelService
    {
        Task<SubscriptionLevelModel> CreateSubscriptionAsync(SubscriptionCreateDto subscriptionLevel);
        Task<SubscriptionLevelModel> UpdateSubscriptionAsync(SubscriptionUpdateDto subscriptionLevel);
        Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync();
        Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsByBlogIdAsync(Guid blogId);
        Task<SubscriptionLevelModel> GetSubscriptionByIdAsync(Guid id);
    }
}
