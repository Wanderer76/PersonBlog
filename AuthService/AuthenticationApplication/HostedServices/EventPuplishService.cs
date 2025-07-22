
using Authentication.Contract.Events;
using Authentication.Domain.Entities;
using MessageBus;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Text.Json;

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
        throw new NotImplementedException();
    }

    private async Task PublishMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IAuthEntity>>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublish>();

            var events = await repository.Get<AuthEvent>()
                .Where(x => x.State == Infrastructure.Models.EventState.Pending)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var @event in events)
            {
                if (@event.EventType == nameof(UserCreateEvent))
                {
                    var data = JsonSerializer.Deserialize<UserCreateEvent>(@event.EventData);
                    await publisher.PublishAsync(data);
                }

                repository.Attach(@event);
                @event.State = Infrastructure.Models.EventState.Processed;
                await repository.SaveChangesAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
