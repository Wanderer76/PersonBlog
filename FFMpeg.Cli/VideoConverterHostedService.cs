using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using RabbitMQ.Client;

namespace VideoProcessing.Cli
{
    internal class VideoConverterHostedService : BackgroundService
    {
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqUploadVideoConfig _config;
        public VideoConverterHostedService(RabbitMqMessageBus messageBus, RabbitMqUploadVideoConfig config)
        {
            _messageBus = messageBus;
            _config = config;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = ProcessUploadVideoEvents();
            return Task.CompletedTask;
        }

        private async Task ProcessUploadVideoEvents()
        {
            var connection = await _messageBus.GetConnectionAsync();
            var videoConverterChannel = await connection.CreateChannelAsync();

            await videoConverterChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await videoConverterChannel.QueueDeclareAsync(_config.VideoProcessQueue, durable: true, exclusive: false, autoDelete: false);

            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.VideoConverterRoutingKey);
            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.FileChunksCombinerRoutingKey);

            try
            {
                await _messageBus.SubscribeAsync(videoConverterChannel,
                    _config.VideoProcessQueue);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
