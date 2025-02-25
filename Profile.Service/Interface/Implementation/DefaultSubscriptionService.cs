using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Interface.Implementation
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

            var hasSubscription = await _readWriteRepository.Get<Subscription>()
                .Where(x => x.ProfileId == profile.Id && x.BlogId == blogId)
                .Where(x => x.SubscriptionEndDate != null)
                .FirstOrDefaultAsync();


            if (hasSubscription != null)
                throw new ArgumentException("У вас уже есть подписка на канал");

            var newSubscription = new Subscription
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
                await _readWriteRepository.Get<Blog>()
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

            var hasActiveSubscription = await _readWriteRepository.Get<Subscription>()
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
                await _readWriteRepository.Get<Blog>()
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
    }
}
