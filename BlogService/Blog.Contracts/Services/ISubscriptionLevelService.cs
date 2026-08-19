using Blog.Contracts.Models;

namespace Blog.Contracts.Services
{
    public interface ISubscriptionLevelService
    {
        Task<global::Result<SubscriptionLevelModel>> CreateSubscriptionAsync(SubscriptionCreateDto subscriptionLevel);
        Task<global::Result<SubscriptionLevelModel>> UpdateSubscriptionAsync(SubscriptionUpdateDto subscriptionLevel);
        Task<global::Result> DeleteSubscriptionAsync(Guid id);
        Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync();
        Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsByBlogIdAsync(Guid blogId);
        Task<global::Result<SubscriptionLevelModel>> GetSubscriptionByIdAsync(Guid id);
    }
}
