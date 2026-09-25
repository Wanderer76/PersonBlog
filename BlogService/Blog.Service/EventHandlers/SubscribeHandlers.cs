using Blog.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Events;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.EventHandlers;

public sealed class SubscribeHandlers : IEventHandler<SubscribeCreateEvent>, IEventHandler<SubscribeCancelEvent>
{
    private readonly IReadWriteRepository<IBlogEntity> _readWriteRepository;

    public SubscribeHandlers(IReadWriteRepository<IBlogEntity> readWriteRepository)
    {
        _readWriteRepository = readWriteRepository;
    }

    public async Task Handle(IMessageContext<SubscribeCreateEvent> @event)
    {
        var blog = await _readWriteRepository.Get<PersonBlog>()
            .FirstOrDefaultAsync(x => x.Id == @event.Message.BlogId);

        if (blog == null || blog.UserId == @event.Message.UserId)
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
        _readWriteRepository.Attach(blog);
        blog.AddSubscriber();
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
        var blog = await _readWriteRepository.Get<PersonBlog>()
            .FirstOrDefaultAsync(x => x.Id == @event.Message.BlogId);
        if (blog != null)
        {
            _readWriteRepository.Attach(blog);
            blog.RemoveSubscriber();
        }
        await _readWriteRepository.SaveChangesAsync();
    }
}
