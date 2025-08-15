using Profile.Domain.Models;
using Shared.Models;

namespace Profile.Domain.Services
{
    public interface ISubscribeService
    {
        Task<HasSubscriptionModel> CheckCurrentUserToSubscriptionAsync(Guid blogId);
        Task<PagedViewModel<SubscribeViewModel>> GetUserSubscriptionListAsync(Guid userId, int page, int size);
        Task SubscribeToBlogAsync(Guid blogId);
        Task UnSubscribeToBlogAsync(Guid blogId);
        //Task SubscribeToPayment(Guid userId, Guid blogId, Guid levelId);
    }
}
