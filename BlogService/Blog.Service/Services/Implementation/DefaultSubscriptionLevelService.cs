using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Infrastructure.Interface;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultSubscriptionLevelService : ISubscriptionLevelService
    {
        private readonly IReadWriteRepository<IBlogEntity> _readWriteRepository;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _userSession;

        public DefaultSubscriptionLevelService(IReadWriteRepository<IBlogEntity> readWriteRepository, ICacheService cacheService, ICurrentUserService userSession)
        {
            _readWriteRepository = readWriteRepository;
            _cacheService = cacheService;
            _userSession = userSession;
        }

        public async Task<SubscriptionLevelModel> CreateSubscriptionAsync(SubscriptionCreateDto subscriptionLevel)
        {
            var currentSubscriptionLevels = await GetSubscriptionsCachedByBlogId(subscriptionLevel.BlogId);
            var previousLevel = currentSubscriptionLevels.FirstOrDefault(x => x.Id == subscriptionLevel.PreviousLevelId);

            if (subscriptionLevel.PreviousLevelId.HasValue && previousLevel == null)
            {
                previousLevel.AssertFound("Не удалось найти уровень подписки");
            }

            if (previousLevel != null && previousLevel!.NextLevelId.HasValue)
            {
                throw new ArgumentException("Уровень уже имеет следующий этап");
            }

            var newLevel = new PaymentSubscription(
                GuidService.GetNewGuid(),
                subscriptionLevel.BlogId,
                subscriptionLevel.Title,
                subscriptionLevel.Description,
                subscriptionLevel.Price,
                subscriptionLevel.PhotoUrl,
                null);

            if (previousLevel != null)
            {
                _readWriteRepository.Attach(previousLevel);
                previousLevel.NextLevelId = newLevel.Id;
            }

            _readWriteRepository.Add(newLevel);
            await _readWriteRepository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync($"{nameof(PaymentSubscription)}:${subscriptionLevel.BlogId}");
            return newLevel.ToLevelModel();
        }

        private async Task<IEnumerable<PaymentSubscription>> GetSubscriptionsCachedByBlogId(Guid blogId)
        {
            var cachedData = await _cacheService.GetCachedDataAsync<IEnumerable<PaymentSubscription>>($"{nameof(PaymentSubscription)}:${blogId}");
            if (cachedData == null)
            {
                var data = await _readWriteRepository.Get<PaymentSubscription>()
                    .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                    .ToListAsync();

                await _cacheService.SetCachedDataAsync($"{nameof(PaymentSubscription)}:${blogId}", data, TimeSpan.FromHours(1));
                cachedData = data;
            }
            return cachedData;
        }

        public async Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync()
        {
            var currentUser = await _userSession.GetUserSessionAsync();

            var blogId = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.UserId == currentUser.UserId)
                .Select(x => x.Id)
                .FirstAsync();

            return (await GetSubscriptionsCachedByBlogId(blogId)).Select(x => x.ToLevelModel());
        }

        public async Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsByBlogIdAsync(Guid blogId)
        {
            return (await GetSubscriptionsCachedByBlogId(blogId)).Select(x => x.ToLevelModel());
        }

        public Task<SubscriptionLevelModel> GetSubscriptionByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<SubscriptionLevelModel> UpdateSubscriptionAsync(SubscriptionUpdateDto subscriptionLevel)
        {
            throw new NotImplementedException();
        }
    }
}
