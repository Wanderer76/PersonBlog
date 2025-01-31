
namespace MessageBus
{
    public interface IMessageBus : IDisposable
    {
        Task SendMessageAsync<T>(string queueName, T message);
        Task SubscribeAsync<T>(string queueName, Func<T, Task> messageHandler);
    }
}