
using Blog.Contracts.Events;
using Infrastructure.Models;
using MessageBus;
using MessageBus.Shared.Configs;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Text.Json;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Events;

namespace VideoReacting.API.HostedService
{
    public class ReactionOutbox : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagePublish _messageBus;

        public ReactionOutbox(IServiceProvider serviceProvider, IMessagePublish messageBus)
        {
            _serviceProvider = serviceProvider;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await using var connection = await _messageBus.GetConnectionAsync();
            //await using var channel = await connection.CreateChannelAsync();
            //await channel.ExchangeDeclareAsync(RabbitMqVideoReactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            //await channel.QueueDeclareAsync(RabbitMqVideoReactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            //await channel.QueueBindAsync(RabbitMqVideoReactionConfig.QueueName, RabbitMqVideoReactionConfig.ExchangeName, RabbitMqVideoReactionConfig.ViewRoutingKey);
            //await channel.ExchangeDeclareAsync(QueueConstants.Exchange, ExchangeType.Direct, durable: true);
            //await channel.QueueDeclareAsync(QueueConstants.QueueName, durable: true, exclusive: false, autoDelete: false);
            //await channel.QueueBindAsync(QueueConstants.QueueName, QueueConstants.Exchange, QueueConstants.RoutingKey);
            //await _messageBus.SubscribeAsync(QueueConstants.QueueName);


            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IUserEntity>>();

                var messages = await dbContext.Get<ReactingEvent>()
                    .Where(m => m.State == EventState.Pending && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        dbContext.Attach(message);
                        await SendEvent(message);
                        message.Processed();
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

        private Task SendEvent(ReactingEvent message)
        {
            switch (message.EventType)
            {
                case nameof(UserReactionSyncEvent):
                    return _messageBus.PublishAsync(QueueConstants.Exchange, RabbitMqVideoReactionConfig.SyncRoutingKey, JsonSerializer.Deserialize<UserReactionSyncEvent>(message.EventData));
                case nameof(UserViewedSyncEvent):
                    return _messageBus.PublishAsync(QueueConstants.Exchange, RabbitMqVideoReactionConfig.SyncRoutingKey, JsonSerializer.Deserialize<UserViewedSyncEvent>(message.EventData));
                case nameof(VideoViewEvent):
                    return _messageBus.PublishAsync(QueueConstants.Exchange, QueueConstants.RoutingKey, JsonSerializer.Deserialize<VideoViewEvent>(message.EventData));
                default:
                    throw new ArgumentException();
            }
        }
    }
}
