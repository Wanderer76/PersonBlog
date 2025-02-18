using RabbitMQ.Client;
using System.Text;
using MessageBus.Configs;
using Shared.Persistence;
using Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MessageBus;
using System.Collections.Concurrent;
using Profile.Domain.Events;
using Infrastructure.Models;
using Xunit.Sdk;
using System.Text.Json;

namespace ProfileApplication.HostedServices
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqUploadVideoConfig _settings;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly ConcurrentDictionary<ulong, ProfileEventMessages> sendMessages = new();

        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            RabbitMqUploadVideoConfig config,
            ILogger<OutboxPublisherService> logger,
            RabbitMqMessageBus messageBus)
        {
            _serviceProvider = serviceProvider;
            _settings = config;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var connection = await _messageBus.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct, durable: true);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                var messages = await dbContext.Get<ProfileEventMessages>()
                    .Where(static m => m.State == EventState.Pending && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    var nextNumber = await channel.GetNextPublishSequenceNumberAsync();
                    try
                    {
                        var body = JsonSerializer.Serialize(message);
                        sendMessages.TryAdd(nextNumber, message);
                        dbContext.Attach(message);
                        message.State = EventState.Processed;
                        await dbContext.SaveChangesAsync();
                        await _messageBus.SendMessageAsync(channel,_settings.ExchangeName,GetRoutingKey(message), message);
                    }
                    catch (Exception ex)
                    {
                        dbContext.Attach(message);
                        sendMessages.Remove(nextNumber, out _);
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

        private async Task ProcessError(ProfileEventMessages message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
            dbContext.Attach(message);
            message.RetryCount++;
            if (message.RetryCount > 3)
            {
                message.SetErrorMessage("Не удалось обработать событие");
                message.State = Infrastructure.Models.EventState.Error;
            }
            else
            {
                message.State = Infrastructure.Models.EventState.Pending;
            }
            await dbContext.SaveChangesAsync();
        }

        private async Task ProcessSuccess(ProfileEventMessages message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
            dbContext.Attach(message);
            message.State = EventState.Complete;
            await dbContext.SaveChangesAsync();
        }

        private string GetRoutingKey(ProfileEventMessages message)
        {
            switch (message.EventType)
            {
                case nameof(VideoUploadEvent):
                    return _settings.VideoConverterRoutingKey;
                case nameof(CombineFileChunksEvent):
                    return _settings.FileChunksCombinerRoutingKey;
                //case nameof(VideoViewEvent):
                //return _settings.FileChunksCombinerRoutingKey;
                default:
                    throw new ArgumentException("Неизвестное событие");
            }
        }
    }
}
