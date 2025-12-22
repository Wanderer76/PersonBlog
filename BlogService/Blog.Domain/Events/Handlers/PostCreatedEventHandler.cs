using Blog.Contracts.Events;
using Blog.Domain.Entities;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.EntityFrameworkCore;
using Notification.Contract.Events;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Domain.Events.Handlers
{
    internal class PostCreatedEventHandler : IEventHandler<PostUpdateEvent>
    {
        private readonly IReadRepository<IBlogEntity> _readRepository;

        public async Task Handle(IMessageContext<PostUpdateEvent> @event)
        {
            if (@event.Message.UpdateType != UpdateType.Create)
                return;

            var offset = 0;
            var takeCount = 1000;

            var subscribers = GetSubscribers(@event, offset, takeCount);
            while(await subscribers.AnyAsync())
            {
                await foreach (var i in subscribers)
                {
                    await @event.PublishAsync(BaseEvent<CreateNotificationEvent>.Create(new CreateNotificationEvent
                    {
                        CreatedAt = DateTimeService.Now(),
                        Payload = "",
                        UserId = i
                    }));
                }
                offset += takeCount;
                subscribers = GetSubscribers(@event, offset, takeCount);
            }
        }

        private IAsyncEnumerable<Guid> GetSubscribers(IMessageContext<PostUpdateEvent> @event, int offset ,int takeCount)
        {
            return _readRepository.Get<Subscriber>()
                            .Where(x => x.BlogId == @event.Message.BlogId)
                            .Skip(offset)
                            .Take(takeCount)
                            .Select(x => x.UserId)
                            .AsAsyncEnumerable();
        }
    }
}
