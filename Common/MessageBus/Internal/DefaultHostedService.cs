using MessageBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Reflection;

namespace MessageBus.Internal
{
    internal class DefaultHostedService : IHostedService, IAsyncDisposable
    {
        private readonly RabbitMqMessageBus _messageBus;
        private readonly MessageBusSubscriptionInfo _subscriptionInfo;

        public DefaultHostedService(RabbitMqMessageBus messageBus, IOptions<MessageBusSubscriptionInfo> subscriptionInfo)
        {
            _messageBus = messageBus;
            _subscriptionInfo = subscriptionInfo.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = ExecuteAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var connection = await _messageBus.GetConnectionAsync();
            using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            var subscribeMethod = typeof(RabbitMqMessageBus).GetMethod(nameof(RabbitMqMessageBus.SubscribeAsync), [typeof(string)]);
            if (subscribeMethod == null)
                throw new InvalidOperationException("SubscribeAsync method not found");
            foreach (var eventType in _subscriptionInfo.Handlers)
            {
                var queue = eventType.Queue;
                var exchange = eventType.Queue.Exchange;
                var attributeData = eventType.HandlerType.GetCustomAttribute<EventPublishAttribute>(false);

                var queueName = queue?.QueueName ?? eventType.HandlerType.FullName!;

                await channel.QueueDeclareAsync(queueName, durable: queue.Durable, exclusive: queue.Exclusive, autoDelete: queue.AutoDelete);

                var exchangeName = exchange?.Name ?? attributeData?.Exchange;

                string? exchangeType;
                if (exchange?.ExchangeType == null)
                {
                    exchangeType = (string?)((attributeData?.RoutingKey != null || exchange?.RoutingKey != null)
                    ? ExchangeType.Direct : ExchangeType.Fanout);
                }
                else
                {
                    exchangeType = (exchange?.ExchangeType);
                }

                var exchangeRoutingKey = exchange?.RoutingKey ?? attributeData?.RoutingKey;
                if (exchangeName != null)
                {
                    await channel.ExchangeDeclareAsync(exchangeName, exchangeType, exchange?.Durable ?? true, exchange?.AutoDelete ?? false);
                    await channel.QueueBindAsync(queueName, exchangeName, exchangeRoutingKey);
                }
                var genericSubscribe = subscribeMethod.MakeGenericMethod(eventType.HandlerType);
                await (Task)genericSubscribe.Invoke(_messageBus, [queueName])!;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async ValueTask DisposeAsync()
        {
            if (_messageBus != null)
            {
                await _messageBus.DisposeAsync();
            }
        }
    }
}
