using MessageBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace MessageBus.Internal
{
    internal class DefaultHostedService : IHostedService, IDisposable
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
            foreach (var eventType in _subscriptionInfo.handlerInfos)
            {
                var queue = eventType.Queue;
                var exchange = eventType.Queue.Exchange;
                if (!string.IsNullOrWhiteSpace(queue.Name))
                {
                    await channel.QueueDeclareAsync(queue.Name, durable: queue.Durable, exclusive: queue.Exclusive, autoDelete: queue.AutoDelete);
                    if (exchange != null)
                    {
                        await channel.ExchangeDeclareAsync(exchange.Name, exchange.ExchangeType, exchange.Durable, exchange.AutoDelete);
                        await channel.QueueBindAsync(queue.Name, exchange.Name, exchange.RoutingKey);
                    }
                    var genericSubscribe = subscribeMethod.MakeGenericMethod(eventType.HanldlerType);
                    await (Task)genericSubscribe.Invoke(_messageBus, [queue.Name])!;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _messageBus?.Dispose();
        }
    }
}
