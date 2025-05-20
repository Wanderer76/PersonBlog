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
using Shared.Utils;

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
                    CorrelationId = message.CorrelationId?.ToString(),
                }, body: body);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public async Task PublishAsync<T>(IChannel channel, string exchangeName, string routingKey, T message, BasicProperties? cfg=null)
        {
            try
            {
                var baseEvent = new BaseEvent { EventData = JsonSerializer.Serialize(message), EventType = typeof(T).Name, };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseEvent));

                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, true, cfg, body: body);
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
                var body = JsonSerializer.Deserialize<BaseEvent>(ea.Body.Span)!;
                using var scope = _serviceScope.CreateScope();

                foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(body.EventType))
                {
                    if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType))
                    {
                        try
                        {
                            var handlerBody = JsonSerializer.Deserialize(body.EventData, eventType)!;
                            Guid? correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId)
                            ? null
                            : Guid.Parse(ea.BasicProperties.CorrelationId);
                            await handler.Handle(new MessageContext<object>(correlationId, handlerBody));
                            await channel.BasicAckAsync(ea.DeliveryTag, false);
                        }
                        catch (Exception e)
                        {
                            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
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
