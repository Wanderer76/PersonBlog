using Blog.Domain.Entities;
using MessageBus;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.Service.Services.Implementation;

public sealed class OutboxPublisher(
    IReadWriteRepository<IBlogEntity> repository,
    IMessagePublish messageBus)
{
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var messages = await repository.Get<VideoProcessEvent>()
            .Where(message =>
                message.State == EventState.Pending
                && message.RetryCount < VideoProcessEvent.MaxPublishAttempts)
            .OrderBy(message => message.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                repository.Attach(message);
                message.Processed();
                await messageBus.PublishAsync(message);
                await repository.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                message.RegisterPublishFailure(exception.Message);
                await repository.SaveChangesAsync();
            }
        }

        return messages.Count;
    }
}
