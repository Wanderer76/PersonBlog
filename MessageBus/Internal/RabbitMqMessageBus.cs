using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Events;
using MessageBus.Configs;
using Infrastructure.Models;
using MessageBus.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using MessageBus.Models;

namespace MessageBus
{
    public class RabbitMqMessageBus
    {
        private readonly IConnectionFactory _factory;
        private readonly IServiceScopeFactory _serviceScope;
        private readonly MessageBusSubscriptionInfo _subscriptionInfo;

        public RabbitMqMessageBus(RabbitMqConnection config, IServiceScopeFactory serviceScope, MessageBusSubscriptionInfo subscriptionInfo)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
            };
            _subscriptionInfo = subscriptionInfo;
            _serviceScope = serviceScope;
        }

        public async Task SendMessageAsync<T>(IChannel channel, string exchangeName, string routingKey, T message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, body: body, mandatory: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Task<IConnection> GetConnectionAsync()
        {
            return _factory.CreateConnectionAsync();
        }

        public async Task SubscribeAsync(IChannel channel, string queueName)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    using var scope = _serviceScope.CreateScope();

                    var routingKey = ea.RoutingKey;
                    var body = JsonSerializer.Deserialize<BaseEvent>(ea.Body.Span)!;
                    foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(body.EventType))
                    {
                        if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType))
                        {
                            var handlerBody = JsonSerializer.Deserialize(body.EventData, eventType)!;
                            await handler.Handle(handlerBody);
                        }
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }
    }
}
