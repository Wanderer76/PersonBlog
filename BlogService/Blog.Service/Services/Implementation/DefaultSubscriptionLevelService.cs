using Blog.Contracts.Models;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
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

        public async Task<Result<SubscriptionLevelModel>> CreateSubscriptionAsync(SubscriptionCreateDto subscriptionLevel)
        {
            var validationError = Validate(subscriptionLevel);
            if (validationError != null)
                return Result<SubscriptionLevelModel>.Failure(validationError);

            var ownershipError = await GetOwnershipErrorAsync(subscriptionLevel.BlogId);
            if (ownershipError != null)
                return Result<SubscriptionLevelModel>.Failure(ownershipError);

            var currentSubscriptionLevels = await _readWriteRepository.Get<PaymentSubscription>()
                .Where(x => x.BlogId == subscriptionLevel.BlogId && !x.IsDeleted)
                .ToListAsync();
            var previousLevel = currentSubscriptionLevels.FirstOrDefault(x => x.Id == subscriptionLevel.PreviousLevelId);

            if (subscriptionLevel.PreviousLevelId.HasValue && previousLevel == null)
            {
                return Result<SubscriptionLevelModel>.Failure(new Error("PreviousLevelId", "Не удалось найти предыдущий уровень подписки"));
            }

            if (previousLevel != null && previousLevel!.NextLevelId.HasValue)
            {
                return Result<SubscriptionLevelModel>.Failure(new Error("PreviousLevelId", "Уровень уже имеет следующий этап"));
            }

            var newLevel = new PaymentSubscription(
                GuidService.GetNewGuid(),
                subscriptionLevel.BlogId,
                subscriptionLevel.Title.Trim(),
                subscriptionLevel.Description?.Trim(),
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
            await _cacheService.RemoveCachedDataAsync(new PaymentSubscriptionCacheKey(subscriptionLevel.BlogId));
            return newLevel.ToLevelModel();
        }

        private async Task<IEnumerable<PaymentSubscription>> GetSubscriptionsCachedByBlogId(Guid blogId)
        {

            var cachedData = await _cacheService.GetOrAddDataAsync(new PaymentSubscriptionCacheKey(blogId), () =>
            {
                return _readWriteRepository.Get<PaymentSubscription>()
                    .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                    .ToListAsync();
            });

            return cachedData;
        }

        public async Task<IEnumerable<SubscriptionLevelModel>> GetAllSubscriptionsAsync()
        {
            var currentUser = await _userSession.GetCurrentUserAsync();

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

        public async Task<Result<SubscriptionLevelModel>> GetSubscriptionByIdAsync(Guid id)
        {
            var level = await _readWriteRepository.Get<PaymentSubscription>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            return level == null
                ? Result<SubscriptionLevelModel>.Failure(new Error("NotFound", "Уровень подписки не найден"))
                : level.ToLevelModel();
        }

        public async Task<Result<SubscriptionLevelModel>> UpdateSubscriptionAsync(SubscriptionUpdateDto subscriptionLevel)
        {
            var validationError = Validate(subscriptionLevel);
            if (validationError != null)
                return Result<SubscriptionLevelModel>.Failure(validationError);

            var level = await _readWriteRepository.Get<PaymentSubscription>()
                .FirstOrDefaultAsync(x => x.Id == subscriptionLevel.Id && !x.IsDeleted);
            if (level == null)
                return Result<SubscriptionLevelModel>.Failure(new Error("NotFound", "Уровень подписки не найден"));

            var ownershipError = await GetOwnershipErrorAsync(level.BlogId);
            if (ownershipError != null)
                return Result<SubscriptionLevelModel>.Failure(ownershipError);

            if (subscriptionLevel.BlogId != level.BlogId)
                return Result<SubscriptionLevelModel>.Failure(new Error("Forbidden", "Нельзя переместить уровень в другой блог"));

            var levelId = level.Id;
            var levels = await _readWriteRepository.Get<PaymentSubscription>()
                .Where(x => x.BlogId == level.BlogId && !x.IsDeleted)
                .ToListAsync();
            level = levels.Single(x => x.Id == levelId);

            if (subscriptionLevel.PreviousLevelId == level.Id)
                return Result<SubscriptionLevelModel>.Failure(new Error("PreviousLevelId", "Уровень не может ссылаться сам на себя"));

            var desiredPrevious = subscriptionLevel.PreviousLevelId.HasValue
                ? levels.FirstOrDefault(x => x.Id == subscriptionLevel.PreviousLevelId.Value)
                : null;
            if (subscriptionLevel.PreviousLevelId.HasValue && desiredPrevious == null)
                return Result<SubscriptionLevelModel>.Failure(new Error("PreviousLevelId", "Не удалось найти предыдущий уровень подписки"));

            var currentPrevious = levels.FirstOrDefault(x => x.NextLevelId == level.Id);
            if (currentPrevious?.Id != desiredPrevious?.Id)
            {
                if (desiredPrevious != null && IsReachable(level.NextLevelId, desiredPrevious.Id, levels))
                    return Result<SubscriptionLevelModel>.Failure(new Error("PreviousLevelId", "Перемещение создаёт цикл уровней подписки"));

                var oldNextLevelId = level.NextLevelId;
                if (currentPrevious != null)
                {
                    _readWriteRepository.Attach(currentPrevious);
                    currentPrevious.NextLevelId = oldNextLevelId;
                }

                _readWriteRepository.Attach(level);
                if (desiredPrevious == null)
                {
                    level.NextLevelId = null;
                }
                else
                {
                    _readWriteRepository.Attach(desiredPrevious);
                    level.NextLevelId = desiredPrevious.NextLevelId;
                    desiredPrevious.NextLevelId = level.Id;
                }
            }
            else
            {
                _readWriteRepository.Attach(level);
            }

            level.Title = subscriptionLevel.Title.Trim();
            level.Description = subscriptionLevel.Description?.Trim();
            level.Price = subscriptionLevel.Price;
            level.ImageId = subscriptionLevel.PhotoUrl;
            level.UpdatedAt = DateTimeService.Now();

            await _readWriteRepository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(new PaymentSubscriptionCacheKey(level.BlogId));
            return level.ToLevelModel();
        }

        public async Task<Result> DeleteSubscriptionAsync(Guid id)
        {
            var level = await _readWriteRepository.Get<PaymentSubscription>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (level == null)
                return Result.Failure(new Error("NotFound", "Уровень подписки не найден"));

            var ownershipError = await GetOwnershipErrorAsync(level.BlogId);
            if (ownershipError != null)
                return Result.Failure(ownershipError);

            var previousLevels = await _readWriteRepository.Get<PaymentSubscription>()
                .Where(x => x.BlogId == level.BlogId && !x.IsDeleted && x.NextLevelId == level.Id)
                .ToListAsync();
            foreach (var previousLevel in previousLevels)
            {
                _readWriteRepository.Attach(previousLevel);
                previousLevel.NextLevelId = level.NextLevelId;
            }

            _readWriteRepository.Attach(level);
            level.NextLevelId = null;
            level.IsDeleted = true;
            level.UpdatedAt = DateTimeService.Now();
            await _readWriteRepository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(new PaymentSubscriptionCacheKey(level.BlogId));
            return Result.Success();
        }

        private async Task<Error?> GetOwnershipErrorAsync(Guid blogId)
        {
            var currentUser = await _userSession.GetCurrentUserAsync();
            var ownerId = await _readWriteRepository.Get<PersonBlog>()
                .Where(x => x.Id == blogId)
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync();

            if (!ownerId.HasValue)
                return new Error("NotFound", "Блог не найден");

            return currentUser.IsAnonymous || ownerId.Value != currentUser.UserId
                ? new Error("Forbidden", "Блог не принадлежит текущему пользователю")
                : null;
        }

        private static Error? Validate(SubscriptionCreateDto subscriptionLevel)
        {
            if (string.IsNullOrWhiteSpace(subscriptionLevel.Title))
                return new Error(nameof(subscriptionLevel.Title), "Название уровня подписки обязательно");

            if (subscriptionLevel.Price < 0 || double.IsNaN(subscriptionLevel.Price) || double.IsInfinity(subscriptionLevel.Price))
                return new Error(nameof(subscriptionLevel.Price), "Стоимость подписки должна быть неотрицательным конечным числом");

            return null;
        }

        private static bool IsReachable(Guid? startId, Guid targetId, IReadOnlyCollection<PaymentSubscription> levels)
        {
            var byId = levels.ToDictionary(x => x.Id);
            var visited = new HashSet<Guid>();
            var currentId = startId;
            while (currentId.HasValue && visited.Add(currentId.Value) && byId.TryGetValue(currentId.Value, out var current))
            {
                if (current.Id == targetId)
                    return true;

                currentId = current.NextLevelId;
            }

            return false;
        }
    }
}
