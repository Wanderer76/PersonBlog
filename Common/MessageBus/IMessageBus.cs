﻿
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
}