using Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using MessageBus;
using Blog.Domain.Entities;
using MessageBus.Models;

namespace Blog.API.HostedServices;

public sealed class OutboxPublisherService(IServiceProvider serviceProvider, IMessagePublish messageBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();

            var messages = await dbContext.Get<VideoProcessEvent>()
                .Where(m => m.State == EventState.Pending && m.RetryCount < VideoProcessEvent.MaxPublishAttempts)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    dbContext.Attach(message);
                    message.Processed();
                    await messageBus.PublishAsync(message);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    dbContext.Attach(message);
                    message.RegisterPublishFailure(ex.Message);
                    await dbContext.SaveChangesAsync();
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
