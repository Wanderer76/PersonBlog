using RabbitMQ.Client;
using Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using MessageBus;
using Infrastructure.Models;
using MessageBus.Shared.Configs;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using MassTransit;
using System.Text.Json;
using ViewReacting.Domain.Events;

namespace Blog.API.HostedServices
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly IChannel _channel;
        private readonly IConnection _connection;
        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            ILogger<OutboxPublisherService> logger,
            RabbitMqMessageBus messageBus)
        {
            _serviceProvider = serviceProvider;
            _connection = messageBus.GetConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _channel.ExchangeDeclareAsync("video-event", "direct", true);
            await _channel.QueueDeclareAsync("saga-queue", true, false, false);
            await _channel.QueueBindAsync("saga-queue", "video-event", "saga");
            await _messageBus.SubscribeAsync("saga-queue");

            await _channel.ExchangeDeclareAsync(QueueConstants.Exchange, ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync(RabbitMqVideoReactionConfig.SyncQueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(RabbitMqVideoReactionConfig.SyncQueueName, QueueConstants.Exchange, RabbitMqVideoReactionConfig.SyncRoutingKey);
            await _messageBus.SubscribeAsync(RabbitMqVideoReactionConfig.SyncQueueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                //var requestClient = scope.ServiceProvider.GetRequiredService<IBus>();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();

                var messages = await dbContext.Get<VideoProcessEvent>()
                    .Where(m => m.State == EventState.Pending && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        dbContext.Attach(message);
                        message.Processed();
                        var command = JsonSerializer.Deserialize<CombineFileChunksCommand>(message.EventData)!;
                        //await requestClient.Publish(command);
                        message.CorrelationId = command.VideoMetadataId;

                        await _messageBus.SendMessageAsync("video-event", "saga", message);
                        await dbContext.SaveChangesAsync();
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
                            message.ResetEvent();
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Интервал опроса
            }
        }
    }
}
