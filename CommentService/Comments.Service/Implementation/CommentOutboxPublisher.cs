using Comments.Domain.Entities;
using MessageBus;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Comments.Service.Implementation;

public sealed class CommentOutboxPublisher(
    IReadWriteRepository<ICommentEntity> repository,
    IMessagePublish messageBus)
{
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var messages = await repository.Get<CommentOutboxMessage>()
            .Where(x => x.State == EventState.Pending && x.RetryCount < CommentOutboxMessage.MaxPublishAttempts)
            .OrderBy(x => x.CreatedAt)
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
