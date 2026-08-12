using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using Profile.Domain.Models;
using Profile.Domain.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Recommendation.Contracts.Events;

namespace Profile.Service.Implementation
{
    internal class DefaultSubscriptionService : ISubscribeService
    {
        private readonly IReadWriteRepository<IUserEntity> _readWriteRepository;
        private readonly ICurrentUserService _userSession;

        public DefaultSubscriptionService(IReadWriteRepository<IUserEntity> readWriteRepository, ICurrentUserService userSession)
        {
            _readWriteRepository = readWriteRepository;
            _userSession = userSession;
        }

        public async Task<HasSubscriptionModel> CheckCurrentUserToSubscriptionAsync(Guid blogId)
        {
            var currentUser = await _userSession.GetCurrentUserAsync();
            var hasSubscription = !currentUser.IsAnonymous
                ? await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == currentUser.UserId)
                .Where(x => x.BlogId == blogId)
                .AnyAsync()
                : false;
            return new HasSubscriptionModel(blogId, hasSubscription);
        }

        public async Task<PagedListViewModel<SubscribeViewModel>> GetUserSubscriptionListAsync(Guid userId, int page, int size)
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

            return new PagedListViewModel<SubscribeViewModel>(pagesCount == 0 ? 1 : (int)pagesCount, size, totalCount, blogs);
        }

        public async Task SubscribeToBlogAsync(Guid blogId)
        {
            var user = await _userSession.GetCurrentUserAsync();
            if (user.IsAnonymous)
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

            var newSubscription = new SubscribedChanel(user.UserId!, blogId);
            _readWriteRepository.Add(newSubscription);
            _readWriteRepository.Add(ReactingEvent.Create(new SubscribeCreateEvent
            {
                BlogId = blogId,
                CreatedAt = newSubscription.CreatedAt,
                UserId = newSubscription.UserId
            }, GuidService.GetNewGuid()));

            var eventId = GuidService.GetNewGuid();
            _readWriteRepository.Add(ReactingEvent.Create(new SubscriptionChangedV1
            {
                EventId = eventId,
                BlogId = blogId,
                UserId = newSubscription.UserId,
                IsSubscribed = true,
                OccurredAt = newSubscription.CreatedAt
            }, eventId));
            await _readWriteRepository.SaveChangesAsync();
        }

        public async Task UnSubscribeToBlogAsync(Guid blogId)
        {
            var user = await _userSession.GetCurrentUserAsync();
            var hasActiveSubscription = await _readWriteRepository.Get<SubscribedChanel>()
                .Where(x => x.UserId == user.UserId && x.BlogId == blogId)
                .FirstOrDefaultAsync();

            if (hasActiveSubscription == null)
                throw new ArgumentException("У вас нет активной подписки на канал");

            _readWriteRepository.Remove(hasActiveSubscription);
            var occurredAt = DateTimeService.Now();
            _readWriteRepository.Add(ReactingEvent.Create(new SubscribeCancelEvent
            {
                UserId = user.UserId,
                CreatedAt = occurredAt,
                BlogId = blogId
            }, GuidService.GetNewGuid()));

            var eventId = GuidService.GetNewGuid();
            _readWriteRepository.Add(ReactingEvent.Create(new SubscriptionChangedV1
            {
                EventId = eventId,
                BlogId = blogId,
                UserId = user.UserId,
                IsSubscribed = false,
                OccurredAt = occurredAt
            }, eventId));
            await _readWriteRepository.SaveChangesAsync();
        }
    }
}
