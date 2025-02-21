using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Events;
using MessageBus.Configs;
using MessageBus.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using MessageBus.Models;
using Microsoft.Extensions.Options;
using Infrastructure.Models;

namespace MessageBus
{
    public class RabbitMqMessageBus
    {
        private readonly IConnectionFactory _factory;
        private readonly IServiceScopeFactory _serviceScope;
        private readonly MessageBusSubscriptionInfo _subscriptionInfo;

        public RabbitMqMessageBus(RabbitMqConnection config, IServiceScopeFactory serviceScope, IOptions<MessageBusSubscriptionInfo> subscriptionInfo)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
            };
            _subscriptionInfo = subscriptionInfo.Value;
            _serviceScope = serviceScope;
        }

        public async Task SendMessageAsync<T>(IChannel channel, string exchangeName, string routingKey, T message) where T : BaseEvent
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, true, new BasicProperties
                {
                    Persistent = true,
                }, body: body);
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

        public async Task SubscribeAsync(IChannel channel, string queueName, SubscribeOptions? options = default)
        {
            options ??= new SubscribeOptions();
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = JsonSerializer.Deserialize<BaseEvent>(ea.Body.Span)!;
                if (options.RoutingKeys == null || options.RoutingKeys.Any() && options.RoutingKeys.Contains(ea.RoutingKey))
                {
                    using var scope = _serviceScope.CreateScope();
                    foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(body.EventType))
                    {
                        if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType))
                        {
                            try
                            {
                                var handlerBody = JsonSerializer.Deserialize(body.EventData, eventType)!;
                                await handler.Handle(handlerBody);
                                await channel.BasicAckAsync(ea.DeliveryTag, false);
                            }
                            catch (Exception e)
                            {
                                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                            }
                        }
                    }
                }
            };
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }
    }

    public class SubscribeOptions
    {
        public HashSet<string> RoutingKeys { get; set; } = [];
    }
}
