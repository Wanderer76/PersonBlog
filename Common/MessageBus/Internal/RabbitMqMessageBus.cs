using Infrastructure.Models;
using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Reflection;
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
        private readonly ConcurrentDictionary<Type, EventPublishAttribute> _cachedValues;
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
            _cachedValues = [];
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

        public async Task PublishAsync<T>(string exchangeName, string routingKey, T message, MessageProperty? cfg = null)
        {
            try
            {
                cfg ??= new MessageProperty();
                using var channel = await _connection.CreateChannelAsync();
                var baseEvent = new BaseEvent<T> { EventData = message, EventType = typeof(T).Name, };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseEvent));
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, true, new BasicProperties
                {
                    CorrelationId = cfg.CorrelationId,
                    Persistent = cfg.Persistence,
                }, body: body);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// если задан конфиг значения аттрибута EventPublishAttribute игнорируются
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="cfg"></param>
        /// <returns></returns>
        public async Task PublishAsync<T>(T message, MessageProperty? cfg = null)
        {
            try
            {
                cfg ??= new MessageProperty();
                ConfigureProperties<T>(cfg);

                using var channel = await _connection.CreateChannelAsync();

                var baseEvent = new BaseEvent<T> { EventData = message, EventType = typeof(T).Name, };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseEvent));

                await channel.BasicPublishAsync(exchange: cfg.Exchange, routingKey: cfg.RoutingKey, true, new BasicProperties
                {
                    CorrelationId = cfg.CorrelationId,
                    Persistent = cfg.Persistence,
                }, body: body);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ConfigureProperties<T>(MessageProperty cfg)
        {
            var type = typeof(T);
            _cachedValues.TryGetValue(type, out EventPublishAttribute? value);
            if (value == null)
            {
                value = type.GetCustomAttribute<EventPublishAttribute>(false);
                _cachedValues.TryAdd(type, value);
            }
            //if (value == null)
            //{
            //    var current = _subscriptionInfo.Handlers.First(x => x.HandlerType == type);
            //    if (cfg.RoutingKey == null)
            //    {
            //        cfg.RoutingKey = current.Queue?.Exchange?.RoutingKey;
            //    }
            //    if (cfg.Exchange == null)
            //    {
            //        cfg.Exchange = current.Queue?.Exchange?.Name;
            //    }
            //}

            var current = _subscriptionInfo.Handlers.FirstOrDefault(x => x.HandlerType == type);
            if (cfg.RoutingKey == null)
            {
                cfg.RoutingKey = value.RoutingKey ?? current?.Queue?.Exchange?.RoutingKey;
            }
            if (cfg.Exchange == null)
            {
                cfg.Exchange = value.Exchange ?? current?.Queue?.Exchange?.Name;
            }

            //if (value != null)
            //{
            //    if (cfg.RoutingKey == null)
            //    {
            //        cfg.RoutingKey = value.RoutingKey;
            //    }

            //    if (cfg.Exchange == null)
            //    {
            //        cfg.Exchange = value.Exchange;
            //    }
            //}
        }

        public Task<IConnection> GetConnectionAsync()
        {
            return _factory.CreateConnectionAsync();
        }

        public async Task SubscribeAsync<T>(string queueName)
        {
            var channel = await _connection.CreateChannelAsync();
            await channel.BasicQosAsync(0, 10, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = JsonSerializer.Deserialize<BaseEvent<T>>(ea.Body.Span)!;
                using var scope = _serviceScope.CreateScope();
                var handlers = scope.ServiceProvider.GetKeyedServices<IEventHandler<T>>(body.EventType);
                if (_subscriptionInfo.EventTypes.TryGetValue(body.EventType, out var eventType) && handlers.Any())
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            var handlerBody = body.EventData; //JsonSerializer.Deserialize<T>(body.EventData)!;
                            Guid? correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId)
                            ? null
                            : Guid.Parse(ea.BasicProperties.CorrelationId);
                            var context = MessageContext.Create(correlationId, handlerBody, this);
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
                }
                else
                    await channel.BasicRejectAsync(ea.DeliveryTag, true);
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
