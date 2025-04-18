using MessageBus;
using RabbitMQ.Client;
using ViewReacting.Domain.Events;

namespace VideoReacting.API.Consumer
{
    public class ViewHandlerHostedService : BackgroundService
    {
        private readonly RabbitMqMessageBus _messageBus;

        public ViewHandlerHostedService(RabbitMqMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await _messageBus.GetConnectionAsync();
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await channel.ExchangeDeclareAsync(QueueConstants.Exchange, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(QueueConstants.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(QueueConstants.QueueName, QueueConstants.Exchange, QueueConstants.RoutingKey);
            await _messageBus.SubscribeAsync(channel, QueueConstants.QueueName);
        }
    }
}
