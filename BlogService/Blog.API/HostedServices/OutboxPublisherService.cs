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

namespace Blog.API.HostedServices
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        //private readonly RabbitMqMessageBus _messageBus;


        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            ILogger<OutboxPublisherService> logger)
        {
            _serviceProvider = serviceProvider;
            //_messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var connection = await _messageBus.GetConnectionAsync();
            //var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            //await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var requestClient = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();

                var messages = await dbContext.Get<VideoProcessEvent>()
                    .Where(m => m.State == EventState.Pending && m.RetryCount < 3 && m.EventType == nameof(CombineFileChunksCommand))
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

                        await requestClient.Publish(command);
                        //await _messageBus.SendMessageAsync(channel, _settings.ExchangeName, GetRoutingKey(message), message);
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

        private async Task ProcessError(VideoProcessEvent message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();
            dbContext.Attach(message);
            message.RetryCount++;
            if (message.RetryCount > 3)
            {
                message.SetErrorMessage("Не удалось обработать событие");
            }
            else
            {
                message.ResetEvent();
            }
            await dbContext.SaveChangesAsync();
        }

        private async Task ProcessSuccess(VideoProcessEvent message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();
            dbContext.Attach(message);
            message.Complete();
            await dbContext.SaveChangesAsync();
        }


        //private string GetRoutingKey(VideoProcessEvent message)
        //{
        //    switch (message.EventType)
        //    {
        //        case nameof(VideoConvertEvent):
        //            return _settings.VideoConverterRoutingKey;
        //        case nameof(CombineFileChunksEvent):
        //            return _settings.FileChunksCombinerRoutingKey;
        //        //case nameof(VideoViewEvent):
        //        //return _settings.FileChunksCombinerRoutingKey;
        //        default:
        //            throw new ArgumentException("Неизвестное событие");
        //    }
        //}
    }
}
