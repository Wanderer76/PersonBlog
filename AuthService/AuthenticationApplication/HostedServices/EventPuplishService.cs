using Authentication.Domain.Entities;
using MessageBus;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace AuthenticationApplication.HostedServices;

public class EventPublishService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public EventPublishService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = PublishMessages(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Gracefully stop the background task
        return Task.CompletedTask;
    }

    private async Task PublishMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IAuthEntity>>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublish>();

            var events = await repository.Get<AuthEvent>()
                .Where(x => x.State == EventState.Pending)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var @event in events)
            {

                repository.Attach(@event);
                @event.Processed();
                await publisher.PublishAsync(@event);
                await repository.SaveChangesAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
