using Infrastructure.Interface;
using MessageBus;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Events;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
{
    internal class DefaultSubscriptionService : ISubscribeService
    {
        private readonly IReadWriteRepository<IUserEntity> _readWriteRepository;
        private readonly IUserSession _userSession;
        private readonly IMessagePublish _messagePublish;
        
        public DefaultSubscriptionService(IReadWriteRepository<IUserEntity> readWriteRepository, IUserSession userSession, IMessagePublish messagePublish)
        {
            _readWriteRepository = readWriteRepository;
            _userSession = userSession;
            _messagePublish = messagePublish;
        }

        public async Task SubscribeToBlogAsync(Guid blogId)
        {
            var user = await _userSession.GetUserSessionAsync();
            if (!user.UserId.HasValue)
            {
                throw new ArgumentException();
            }
            if (user.BlogId == blogId)
            {
                throw new ArgumentException("Вы не можете подписаться на свой канал");
            }

            var hasSubscription = await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == user.UserId && x.BlogId == blogId)
                .FirstOrDefaultAsync();

            if (hasSubscription != null)
                throw new ArgumentException("У вас уже есть подписка на канал");

            var newSubscription = new SubscribedChanel(user.UserId!.Value, blogId);
            _readWriteRepository.Add(newSubscription);
            await _readWriteRepository.SaveChangesAsync();
            await _messagePublish.PublishAsync(new SubscribeCreateEvent
            {
                BlogId = blogId,
                CreatedAt = newSubscription.CreatedAt,
                UserId = newSubscription.UserId
            });
        }

        public async Task UnSubscribeToBlogAsync(Guid blogId)
        {
            var user = await _userSession.GetUserSessionAsync();
            var hasActiveSubscription = await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == user.UserId.Value && x.BlogId == blogId)
                .FirstOrDefaultAsync();

            if (hasActiveSubscription == null)
                throw new ArgumentException("У вас нет активной подписки на канал");

            _readWriteRepository.Remove(hasActiveSubscription);
            await _readWriteRepository.SaveChangesAsync();
            await _messagePublish.PublishAsync(new SubscribeCancelEvent
            {
                UserId = user.UserId.Value,
                CreatedAt = DateTimeService.Now(),
                BlogId = blogId
            });
        }
    }
}
