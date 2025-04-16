using MessageBus;
using MessageBus.Shared.Configs;
using RabbitMQ.Client;

namespace ReactionProcessing.Cli.HostedServices
{
    public class VideoReactionProcessingHostedService : BackgroundService
    {
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqVideoReactionConfig _config = new();
        private IChannel channel;

        public VideoReactionProcessingHostedService(RabbitMqMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = ProcessVideoReactionsEvents();
            return Task.CompletedTask;
        }

        private async Task ProcessVideoReactionsEvents()
        {
            var connection = await _messageBus.GetConnectionAsync();
            channel ??= await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_config.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_config.QueueName, _config.ExchangeName, _config.ViewRoutingKey);

            try
            {
                await _messageBus.SubscribeAsync(channel, _config.QueueName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
