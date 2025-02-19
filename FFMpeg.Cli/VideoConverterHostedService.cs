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
            var videoConverterErrorChannel = await connection.CreateChannelAsync();

            await videoConverterChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await videoConverterChannel.QueueDeclareAsync(_config.VideoProcessQueue, durable: true, exclusive: false, autoDelete: false);
            
            await videoConverterErrorChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await videoConverterErrorChannel.QueueDeclareAsync(_config.VideoProcessErrorQueue, durable: true, exclusive: false, autoDelete: false);

            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.VideoConverterRoutingKey);
            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.FileChunksCombinerRoutingKey);

            await videoConverterErrorChannel.QueueBindAsync(_config.VideoProcessErrorQueue, _config.ExchangeName, _config.ErrorRoutingKey);
            try
            {
                await _messageBus.SubscribeAsync(videoConverterChannel, _config.VideoProcessQueue, async (@event, exception) =>
                {
                    @event.SetErrorMessage(exception.Message);
                    await _messageBus.SendMessageAsync(videoConverterErrorChannel, _config.ExchangeName, _config.ErrorRoutingKey, @event);
                });
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
