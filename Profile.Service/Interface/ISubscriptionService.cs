namespace Profile.Service.Interface
{
    public interface ISubscriptionService
    {
        Task SubscribeToBlogAsync(Guid blogId, Guid userId);
        Task UnSubscribeToBlogAsync(Guid blogId, Guid userId);
    }
}
