using MessageBus.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
            foreach (var eventType in _subscriptionInfo.handlerInfos)
            {
                var queue = eventType.Queue;
                var exchange = eventType.Queue.Exchange;
                await channel.QueueDeclareAsync(queue.Name, durable: queue.Durable, exclusive: queue.Exclusive, autoDelete: queue.AutoDelete);
                if (exchange != null)
                {
                    await channel.ExchangeDeclareAsync(exchange.Name, exchange.ExchangeType, exchange.Durable, exchange.AutoDelete);
                    await channel.QueueBindAsync(queue.Name, exchange.Name, exchange.RoutingKey);
                }
                await _messageBus.SubscribeAsync(eventType.Queue.Name);
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
