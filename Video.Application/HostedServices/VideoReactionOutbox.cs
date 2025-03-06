using Infrastructure.Models;
using MessageBus;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Persistence;
using Video.Domain.Entities;
using Video.Domain.Events;

namespace VideoView.Application.HostedServices
{
    public class VideoReactionOutbox : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqVideoReactionConfig _settings = new();
        private readonly RabbitMqMessageBus _messageBus;

        public VideoReactionOutbox(IServiceProvider serviceProvider, RabbitMqMessageBus messageBus)
        {
            _serviceProvider = serviceProvider;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var connection = await _messageBus.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, _settings.ViewRoutingKey);

            await channel.QueueDeclareAsync(_settings.SyncQueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_settings.SyncQueueName, _settings.ExchangeName, _settings.SyncRoutingKey);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IVideoViewEntity>>();

                var messages = await dbContext.Get<VideoEvent>()
                    .Where(static m => m.State == EventState.Pending && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    var nextNumber = await channel.GetNextPublishSequenceNumberAsync();
                    try
                    {
                        dbContext.Attach(message);
                        message.State = EventState.Processed;
                        await dbContext.SaveChangesAsync();

                        var (queueName, routingKey) = GetRoutingKeyWithQueue(message);

                        await _messageBus.SendMessageAsync(channel, _settings.ExchangeName, routingKey, message);
                    }
                    catch (Exception ex)
                    {
                        dbContext.Attach(message);
                        if (message.RetryCount == 3)
                        {
                            message.SetErrorMessage(ex.Message);
                        }
                        else
                        {
                            message.RetryCount++;
                            message.State = EventState.Pending;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Интервал опроса
            }
        }

        private (string QueueName, string RoutingKey) GetRoutingKeyWithQueue(VideoEvent message)
        {
            switch (message.EventType)
            {
                case nameof(UserViewedPostEvent):
                    return (_settings.QueueName, _settings.ViewRoutingKey);
                case nameof(UserViewedSyncEvent):
                    return (_settings.SyncQueueName, _settings.SyncRoutingKey);
                default:
                    throw new ArgumentException();
            }
        }
    }
}
