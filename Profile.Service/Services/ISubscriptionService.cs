namespace Blog.Service.Services
{
    public interface ISubscriptionService
    {
        Task SubscribeToBlogAsync(Guid blogId, Guid userId);
        Task UnSubscribeToBlogAsync(Guid blogId, Guid userId);
        Task SubscribeToLevel(Guid userId, Guid blogId, Guid levelId);
    }
}