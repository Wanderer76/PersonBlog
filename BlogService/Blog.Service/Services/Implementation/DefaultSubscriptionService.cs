using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultSubscriptionService : ISubscriptionService
    {
        private readonly IReadWriteRepository<IBlogEntity> _readWriteRepository;

        public DefaultSubscriptionService(IReadWriteRepository<IBlogEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task SubscribeToBlogAsync(Guid blogId, Guid userId)
        {
            var isCurrentUserBlog = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.Id == blogId && x.UserId == userId)
                .AnyAsync();

            if (isCurrentUserBlog)
            {
                throw new ArgumentException("Вы не можете подписаться на свой канал");
            }

            var hasSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.UserId == userId && x.BlogId == blogId)
                .Where(x => x.SubscriptionEndDate == null)
                .FirstOrDefaultAsync();

            if (hasSubscription != null)
                throw new ArgumentException("У вас уже есть подписка на канал");

            var newSubscription = new Subscriber
            {
                Id = GuidService.GetNewGuid(),
                BlogId = blogId,
                UserId = userId,
                SubscriptionStartDate = DateTimeOffset.UtcNow,
            };
            using var transaction = await _readWriteRepository.BeginTransactionAsync();
            try
            {
                _readWriteRepository.Add(newSubscription);
                await _readWriteRepository.Get<PersonBlog>()
                   .Where(x => x.Id == blogId)
                   .ExecuteUpdateAsync(blog => blog.SetProperty(u => u.SubscriptionsCount, u => u.SubscriptionsCount + 1));
                await _readWriteRepository.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
            }
        }

        public async Task UnSubscribeToBlogAsync(Guid blogId, Guid userId)
        {
            var hasActiveSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.UserId == userId && x.BlogId == blogId)
                .Where(x => x.SubscriptionEndDate == null)
                .FirstOrDefaultAsync();

            if (hasActiveSubscription == null)
                throw new ArgumentException("У вас нет активной подписки на канал");

            using var transaction = await _readWriteRepository.BeginTransactionAsync();
            try
            {
                _readWriteRepository.Attach(hasActiveSubscription);
                hasActiveSubscription.SubscriptionEndDate = DateTimeOffset.UtcNow;
                await _readWriteRepository.Get<PersonBlog>()
                   .Where(x => x.Id == blogId)
                   .ExecuteUpdateAsync(blog => blog.SetProperty(u => u.SubscriptionsCount, u => u.SubscriptionsCount - 1));
                await _readWriteRepository.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
            }
        }

        public async Task SubscribeToPayment(Guid userId, Guid blogId, Guid levelId)
        {
            var subscriptionLevels = await _readWriteRepository.Get<PaymentSubscription>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .ToListAsync();

            var subscriptions = await _readWriteRepository.Get<ProfileSubscription>()
                .Where(x => x.UserId == userId)
                .Where(s => s.SubscriptionLevel.BlogId == blogId && s.IsActive)
                .ToListAsync();

            if (subscriptions.Any(x => x.SubscriptionLevelId == levelId))
            {
                throw new ArgumentException("Уже есть активная подписка");
            }

            var selectedSubscriptionLevel = subscriptionLevels
                .Where(x => x.NextLevelId == levelId)
                .FirstOrDefault();

            if (selectedSubscriptionLevel != null && subscriptions.Any(x => x.SubscriptionLevelId == selectedSubscriptionLevel.Id))
            {
                var old = subscriptions.First(x => x.SubscriptionLevelId == selectedSubscriptionLevel.Id);
                _readWriteRepository.Attach(old);
                old.UpdatedAt = DateTimeService.Now();
                old.IsActive = false;
            }

            var result = new ProfileSubscription(userId, levelId);
            _readWriteRepository.Add(result);
            await _readWriteRepository.SaveChangesAsync();
        }
    }
}
