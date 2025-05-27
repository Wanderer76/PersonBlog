using MessageBus;
using MessageBus.Configs;
using MessageBus.Shared.Configs;
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

            await videoConverterChannel.ExchangeDeclareAsync("video-event", ExchangeType.Direct, durable: true);
            await videoConverterChannel.QueueDeclareAsync("video-convert", durable: true, exclusive: false, autoDelete: false);
            await videoConverterChannel.QueueDeclareAsync("combine-chunks", durable: true, exclusive: false, autoDelete: false);

            await videoConverterChannel.QueueBindAsync("combine-chunks", "video-event", "chunks.combine");
            await videoConverterChannel.QueueBindAsync("video-convert", "video-event", "video.convert");
            await _messageBus.SubscribeAsync("video-convert");
            await _messageBus.SubscribeAsync("combine-chunks");
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
