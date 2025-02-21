using MessageBus.Shared.Configs;
using MessageBus;
using RabbitMQ.Client;

namespace ProfileApplication.HostedServices
{
    public class SyncProfileHostedService : BackgroundService
    {
        private readonly RabbitMqVideoReactionConfig _reactingSettings = new();
        private readonly RabbitMqMessageBus _messageBus;
        private IChannel _channel;

        public SyncProfileHostedService(RabbitMqVideoReactionConfig reactingSettings, RabbitMqMessageBus messageBus)
        {
            _reactingSettings = reactingSettings;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await _messageBus.GetConnectionAsync();
            _channel ??= await connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync(_reactingSettings.ExchangeName, ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync(_reactingSettings.SyncQueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(_reactingSettings.SyncQueueName, _reactingSettings.ExchangeName, _reactingSettings.SyncRoutingKey);
            await _messageBus.SubscribeAsync(_channel, _reactingSettings.SyncQueueName, new SubscribeOptions
            {
                RoutingKeys = [_reactingSettings.SyncRoutingKey]
            });
        }
    }
}
