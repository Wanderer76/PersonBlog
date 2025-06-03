using Infrastructure.Models;
using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MessageBus
{
    internal class RabbitMqMessageBus : IMessagePublish, IDisposable
    {
        private readonly IConnectionFactory _factory;
        private readonly IServiceScopeFactory _serviceScope;
        private readonly MessageBusSubscriptionInfo _subscriptionInfo;
        private readonly IConnection _connection;
        private readonly List<IChannel> _channels;

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
            _connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channels = new List<IChannel>();
        }

        public async Task SendMessageAsync<T>(string exchangeName, string routingKey, T message) where T : BaseEvent
        {
            try
            {
                using var channel = await _connection.CreateChannelAsync();
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

        public async Task PublishAsync<T>(string exchangeName, string routingKey, T message, BasicProperties? cfg = null)
        {
            try
            {
                using var channel = await _connection.CreateChannelAsync();
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

        //public async Task SubscribeAsync(string queueName)
        //{
        //    var channel = await _connection.CreateChannelAsync();
        //    await channel.BasicQosAsync(0, 10, false);
        //    var consumer = new AsyncEventingBasicConsumer(channel);
        //    consumer.ReceivedAsync += async (model, ea) =>
        //    {
        //        var body = JsonSerializer.Deserialize<BaseEvent>(ea.Body.Span)!;
        //        using var scope = _serviceScope.CreateScope();

        //        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(body.EventType))
        //        {
        //            if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType))
        //            {
        //                try
        //                {
        //                    var handlerBody = JsonSerializer.Deserialize(body.EventData, eventType)!;
        //                    Guid? correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId)
        //                    ? null
        //                    : Guid.Parse(ea.BasicProperties.CorrelationId);
        //                    var context = new MessageContext(correlationId, handlerBody);
        //                    await handler.Handle(context);
        //                    await channel.BasicAckAsync(ea.DeliveryTag, false);
        //                }
        //                catch (Exception e)
        //                {
        //                    await channel.BasicPublishAsync("error", "", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        //                    {
        //                        Body = body,
        //                        Error = e
        //                    })));
        //                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
        //                }
        //            }
        //            else
        //                await channel.BasicRejectAsync(ea.DeliveryTag, true);
        //        }
        //    };
        //    await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        //    _channels.Add(channel);
        //}

        public async Task SubscribeAsync<T>(string queueName)
        {
            var channel = await _connection.CreateChannelAsync();
            await channel.BasicQosAsync(0, 10, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = JsonSerializer.Deserialize<BaseEvent>(ea.Body.Span)!;
                using var scope = _serviceScope.CreateScope();

                foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler<T>>(body.EventType))
                {
                    if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType))
                    {
                        try
                        {
                            var handlerBody = JsonSerializer.Deserialize<T>(body.EventData)!;
                            Guid? correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId)
                            ? null
                            : Guid.Parse(ea.BasicProperties.CorrelationId);
                            var context = new MessageContext<T>(correlationId, handlerBody);
                            await handler.Handle(context);
                            await channel.BasicAckAsync(ea.DeliveryTag, false);
                        }
                        catch (Exception e)
                        {
                            await channel.BasicPublishAsync("error", "", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                            {
                                Body = body,
                                Error = e
                            })));
                            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        }
                    }
                    else
                        await channel.BasicRejectAsync(ea.DeliveryTag, true);
                }
            };
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
            _channels.Add(channel);
        }

        public void Dispose()
        {
            foreach (var i in _channels)
            {
                i?.CloseAsync().GetAwaiter().GetResult();
                i?.Dispose();
            }
            _connection?.Dispose();
        }
    }
}
