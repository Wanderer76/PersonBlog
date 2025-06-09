namespace ViewReacting.Domain.Services
{
    public interface ISubscribeService
    {
            Task SubscribeToBlogAsync(Guid blogId);
            Task UnSubscribeToBlogAsync(Guid blogId);
            //Task SubscribeToPayment(Guid userId, Guid blogId, Guid levelId);
    }
}
