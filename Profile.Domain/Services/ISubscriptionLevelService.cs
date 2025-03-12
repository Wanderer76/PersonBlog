using Profile.Domain.Entities;
using Profile.Domain.Services.Models;

namespace Profile.Domain.Services
{
    public interface ISubscriptionLevelService
    {
        Task<SubscriptionLevelModel> CreateSubscriptionAsync(SubscriptionCreateDto subscriptionLevel);
        Task<SubscriptionLevelModel> UpdateSubscriptionAsync(SubscriptionUpdateDto subscriptionLevel);
        Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync();
    }
}
