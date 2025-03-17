using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Infrastructure.Cache.Services;
using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultSubscriptionLevelService : ISubscriptionLevelService
    {
        private readonly IReadWriteRepository<IProfileEntity> _readWriteRepository;
        private readonly ICacheService _cacheService;
        private readonly IUserSession _userSession;

        public DefaultSubscriptionLevelService(IReadWriteRepository<IProfileEntity> readWriteRepository, ICacheService cacheService, IUserSession userSession)
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

            var newLevel = new SubscriptionLevel(
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
            await _cacheService.RemoveCachedDataAsync($"{nameof(SubscriptionLevel)}:${subscriptionLevel.BlogId}");
            return newLevel.ToLevelModel();
        }

        private async Task<IEnumerable<SubscriptionLevel>> GetSubscriptionsCachedByBlogId(Guid blogId)
        {
            var cachedData = await _cacheService.GetCachedDataAsync<IEnumerable<SubscriptionLevel>>($"{nameof(SubscriptionLevel)}:${blogId}");
            if (cachedData == null)
            {
                var data = await _readWriteRepository.Get<SubscriptionLevel>()
                    .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                    .ToListAsync();

                await _cacheService.SetCachedDataAsync($"{nameof(SubscriptionLevel)}:${blogId}", data, TimeSpan.FromHours(1));
                cachedData = data;
            }
            return cachedData;
        }

        public async Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync()
        {
            var currentUser = await _userSession.GetUserSessionAsync();

            var blogId = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.Profile.UserId == currentUser.UserId)
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
