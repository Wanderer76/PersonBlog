namespace Profile.Service.Services
{
    public interface ISubscriptionService
    {
        Task SubscribeToBlogAsync(Guid blogId, Guid userId);
        Task UnSubscribeToBlogAsync(Guid blogId, Guid userId);
    }
}
