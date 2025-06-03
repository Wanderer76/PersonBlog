
using Infrastructure.Models;
using RabbitMQ.Client;

namespace MessageBus
{
    //public interface IMessageBus : IDisposable
    //{
    //    Task SendMessageAsync<T>(string exchangeName, string routingKey, T message, Action<T> onReceived);
    //    Task SubscribeAsync<T>(string queueName, Func<T, Task> messageHandler);
    //    Task SubscribeAsync<T>(IChannel connection, string queueName, Func<T, Task> messageHandler);
    //    Task<IConnection> GetConnectionAsync();
    //}

    public interface IMessagePublish
    {
        Task SendMessageAsync<T>(string exchangeName, string routingKey, T message) where T : BaseEvent;
        Task PublishAsync<T>(string exchangeName, string routingKey, T message, BasicProperties? cfg = null);
    }
}