using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultSubscriptionService : ISubscriptionService
    {
        private readonly IReadWriteRepository<IProfileEntity> _readWriteRepository;

        public DefaultSubscriptionService(IReadWriteRepository<IProfileEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task SubscribeToBlogAsync(Guid blogId, Guid userId)
        {
            var profile = await _readWriteRepository.Get<AppProfile>()
                .FirstAsync(x => x.UserId == userId);

            var isCurrentUserBlog = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.Id == blogId && x.ProfileId == profile.Id)
                .AnyAsync();

            if (isCurrentUserBlog)
            {
                throw new ArgumentException("Вы не можете подписаться на свой канал");
            }

            var hasSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.ProfileId == profile.Id && x.BlogId == blogId)
                .Where(x => x.SubscriptionEndDate == null)
                .FirstOrDefaultAsync();

            if (hasSubscription != null)
                throw new ArgumentException("У вас уже есть подписка на канал");

            var newSubscription = new Subscriber
            {
                Id = GuidService.GetNewGuid(),
                BlogId = blogId,
                ProfileId = profile.Id,
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
            var profile = await _readWriteRepository.Get<AppProfile>()
                          .FirstAsync(x => x.UserId == userId);

            var hasActiveSubscription = await _readWriteRepository.Get<Subscriber>()
                .Where(x => x.ProfileId == profile.Id && x.BlogId == blogId)
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

        public async Task SubscribeToLevel(Guid userId, Guid blogId, Guid levelId)
        {
            var subscriptionLevels = await _readWriteRepository.Get<SubscriptionLevel>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .ToListAsync();

            var profile = await _readWriteRepository.Get<AppProfile>()
                .Where(x => x.UserId == userId)
                .Include(x => x.PaymentSubscriptions.Where(s => s.SubscriptionLevel.BlogId == blogId && s.IsActive))
                .FirstAsync();

            var subscriptions = profile.PaymentSubscriptions.ToList();

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

            var result = new ProfileSubscription(profile.Id, levelId);
            _readWriteRepository.Add(result);
            await _readWriteRepository.SaveChangesAsync();
        }
    }
}
