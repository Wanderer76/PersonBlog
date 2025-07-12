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
using Blog.Contracts.Events;

namespace Blog.API.HostedServices
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagePublish _messageBus;
        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            ILogger<OutboxPublisherService> logger,
            IMessagePublish messageBus)
        {
            _serviceProvider = serviceProvider;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
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
                        if (message.EventType == nameof(CombineFileChunksCommand))
                        {
                            var command = JsonSerializer.Deserialize<CombineFileChunksCommand>(message.EventData)!;
                            await _messageBus.PublishAsync(command);
                        }
                        if(message.EventType == nameof(PostUpdateEvent))
                        {
                            var command = JsonSerializer.Deserialize<PostUpdateEvent>(message.EventData)!;
                            await _messageBus.PublishAsync(command);
                        }
                        if (message.EventType == nameof(BlogCreateEvent))
                        {
                            var command = JsonSerializer.Deserialize<BlogCreateEvent>(message.EventData)!;
                            await _messageBus.PublishAsync(command);
                        }

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
