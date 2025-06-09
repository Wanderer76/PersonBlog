using Blog.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using ViewReacting.Domain.Events;

namespace Blog.API.Handlers
{
    public class SubscribeHandlers : IEventHandler<SubscribeCreateEvent>, IEventHandler<SubscribeCancelEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _readWriteRepository;

        public SubscribeHandlers(IReadWriteRepository<IBlogEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task Handle(IMessageContext<SubscribeCreateEvent> @event)
        {
            var isCurrentUserBlog = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.Id == @event.Message.BlogId && x.UserId == @event.Message.UserId)
                .AnyAsync();

            if (isCurrentUserBlog)
            {
                return;
            }

            var hasSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.UserId == @event.Message.UserId && x.BlogId == @event.Message.BlogId)
                .Where(x => x.SubscriptionEndDate == null)
                .FirstOrDefaultAsync();

            if (hasSubscription != null)
            {
                return;
            }
            var newSubscription = new Subscriber
            {
                Id = GuidService.GetNewGuid(),
                BlogId = @event.Message.BlogId,
                UserId = @event.Message.UserId,
                SubscriptionStartDate = @event.Message.CreatedAt,
            };
            _readWriteRepository.Add(newSubscription);
            await _readWriteRepository.Get<PersonBlog>()
               .Where(x => x.Id == newSubscription.BlogId)
               .ExecuteUpdateAsync(blog => blog.SetProperty(u => u.SubscriptionsCount, u => u.SubscriptionsCount + 1));
            await _readWriteRepository.SaveChangesAsync();

        }
        public async Task Handle(IMessageContext<SubscribeCancelEvent> @event)
        {
            var hasActiveSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.UserId == @event.Message.UserId && x.BlogId == @event.Message.BlogId)
                .Where(x => x.SubscriptionEndDate == null)
                .FirstOrDefaultAsync();

            if (hasActiveSubscription == null)
                return;

            _readWriteRepository.Attach(hasActiveSubscription);
            hasActiveSubscription.SubscriptionEndDate = @event.Message.CreatedAt;
            await _readWriteRepository.Get<PersonBlog>()
               .Where(x => x.Id == @event.Message.BlogId)
               .ExecuteUpdateAsync(blog => blog.SetProperty(u => u.SubscriptionsCount, u => u.SubscriptionsCount - 1));
            await _readWriteRepository.SaveChangesAsync();
        }
    }
}
