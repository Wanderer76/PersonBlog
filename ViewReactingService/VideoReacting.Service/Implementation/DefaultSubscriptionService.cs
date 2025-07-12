using Infrastructure.Interface;
using MessageBus;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Events;
using ViewReacting.Domain.Models;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
{
    internal class DefaultSubscriptionService : ISubscribeService
    {
        private readonly IReadWriteRepository<IUserEntity> _readWriteRepository;
        private readonly ICurrentUserService _userSession;
        private readonly IMessagePublish _messagePublish;

        public DefaultSubscriptionService(IReadWriteRepository<IUserEntity> readWriteRepository, ICurrentUserService userSession, IMessagePublish messagePublish)
        {
            _readWriteRepository = readWriteRepository;
            _userSession = userSession;
            _messagePublish = messagePublish;
        }

        public async Task<HasSubscriptionModel> CheckCurrentUserToSubscriptionAsync(Guid blogId)
        {
            var currentUser = await _userSession.GetCurrentUserAsync();
            var hasSubscription = currentUser.UserId.HasValue
                ? await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == currentUser.UserId.Value)
                .Where(x => x.BlogId == blogId)
                .AnyAsync()
                : false;
            return new HasSubscriptionModel(blogId, hasSubscription);
        }

        public async Task<PagedViewModel<SubscribeViewModel>> GetUserSubscriptionListAsync(Guid userId, int page, int size)
        {
            var totalCount = await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == userId)
                .CountAsync();

            var blogs = await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new SubscribeViewModel(x.BlogId, x.CreatedAt))
                .AsAsyncEnumerable()
                .ToListAsync();

            var pagesCount = Math.Ceiling(totalCount / (double)size);

            return new PagedViewModel<SubscribeViewModel>(pagesCount == 0 ? 1 : (int)pagesCount, totalCount, blogs);
        }

        public async Task SubscribeToBlogAsync(Guid blogId)
        {
            var user = await _userSession.GetCurrentUserAsync();
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
            var user = await _userSession.GetCurrentUserAsync();
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
