using Blog.Domain.Entities;
using Blog.Domain.Events;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Shared.Configs;
using RabbitMQ.Client;

namespace Blog.API
{
    public class VideoCreateSaga : IEventHandler<VideoProcessEvent>
    {
        private readonly RabbitMqUploadVideoConfig _settings;
        private readonly RabbitMqMessageBus _messageBus;

        public Task Handle(VideoProcessEvent @event)
        {
            throw new NotImplementedException();
        }

        public async Task StartProcess(VideoProcessEvent videoConvertEvent)
        {
            var connection = await _messageBus.GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_settings.ProcessingEventResultQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_settings.ProcessingEventResultQueue, _settings.ExchangeName, _settings.ProcessingEventResultRoutingKey);
            await _messageBus.SubscribeAsync(channel, _settings.ProcessingEventResultQueue);
        }
    }
}
