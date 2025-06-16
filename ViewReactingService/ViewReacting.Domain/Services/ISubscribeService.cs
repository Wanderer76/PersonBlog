using Shared.Models;
using ViewReacting.Domain.Models;

namespace ViewReacting.Domain.Services
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
