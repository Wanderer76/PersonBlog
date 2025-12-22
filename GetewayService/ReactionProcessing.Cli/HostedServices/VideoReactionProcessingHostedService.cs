using MessageBus;
using MessageBus.Shared.Configs;
using RabbitMQ.Client;

namespace ReactionProcessing.Cli.HostedServices
{
    public class VideoReactionProcessingHostedService : BackgroundService
    {
        private readonly RabbitMqMessageBus _messageBus;
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
            using var connection = await _messageBus.GetConnectionAsync();
            channel ??= await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(RabbitMqVideoReactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(RabbitMqVideoReactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(RabbitMqVideoReactionConfig.QueueName, RabbitMqVideoReactionConfig.ExchangeName, RabbitMqVideoReactionConfig.ViewRoutingKey);

            try
            {
                await _messageBus.SubscribeAsync(RabbitMqVideoReactionConfig.QueueName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
