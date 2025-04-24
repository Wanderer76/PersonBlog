using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Models;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
{
    internal class DefaultViewHistoryService : IViewHistoryService
    {
        private readonly IReadWriteRepository<IVideoReactEntity> _repository;
        private readonly ICacheService _cacheService;

        public DefaultViewHistoryService(IReadWriteRepository<IVideoReactEntity> repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<Result<UpdateViewState>> CreateOrUpdateViewHistory(UserPostView postViewer)
        {
            var lastPreviousView = await _repository.Get<UserPostView>()
            .Where(x => x.UserId == postViewer.UserId)
                 .Where(x => x.PostId == postViewer.PostId)
                 .OrderByDescending(x => x.WatchedAt)
                 .FirstOrDefaultAsync();
            var state = UpdateViewState.Created;
            if (lastPreviousView != null && lastPreviousView.WatchedAt.Date == DateTimeService.Now().Date)
            {
                _repository.Attach(lastPreviousView);
                lastPreviousView.WatchedTime = postViewer.WatchedTime;
                lastPreviousView.IsCompleteWatch = postViewer.IsCompleteWatch;
                state = UpdateViewState.Updated;
            }
            else
            {
                _repository.Add(new UserPostView(postViewer.UserId, postViewer.PostId, postViewer.WatchedTime, postViewer.IsCompleteWatch));
            }
            await _repository.SaveChangesAsync();
            return Result<UpdateViewState>.Success(state);
        }

        public async Task<Result<HistoryViewItem>> GetUserViewHistoryItemAsync(Guid postId, Guid userId)
        {
            var key = new UserPostViewCacheKey(userId);
            var cacheData = await _cacheService.GetCachedDataAsync<List<HistoryViewItem>>(key);
            if (cacheData != null)
            {
                return Result<HistoryViewItem>.Success(cacheData.FirstOrDefault(x => x.PostId == postId));
            }
            var lastPreviousView = await _repository.Get<UserPostView>()
                .Where(x => x.UserId == userId)
                .Where(x => x.PostId == postId)
                .OrderByDescending(x => x.WatchedAt)
                  .Select(x => new HistoryViewItem
                  {
                      Id = x.Id,
                      LastWatched = x.WatchedAt.DateTime,
                      WatchedTime = x.WatchedTime,
                      PostId = x.PostId
                  })
                .FirstOrDefaultAsync();

            return Result<HistoryViewItem>.Success(lastPreviousView?? new());
        }

        public async Task<Result<IReadOnlyList<HistoryViewItem>>> GetUserViewHistoryListAsync(Guid userId)
        {
            var key = new UserPostViewCacheKey(userId);
            var cacheData = await _cacheService.GetCachedDataAsync<List<HistoryViewItem>>(key);
            if (cacheData != null)
            {
                return Result<IReadOnlyList<HistoryViewItem>>.Success(cacheData);
            }

            var lastPreviousView = await _repository.Get<UserPostView>()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.WatchedAt)
                .AsAsyncEnumerable()
                .Select(x => new HistoryViewItem
                {
                    Id = x.Id,
                    LastWatched = x.WatchedAt.DateTime,
                    WatchedTime = x.WatchedTime,
                    PostId = x.PostId
                })
                .ToListAsync();

            if (lastPreviousView.Count > 0)
            {
                await _cacheService.SetCachedDataAsync(key, lastPreviousView, TimeSpan.FromMinutes(5));
            }

            return Result<IReadOnlyList<HistoryViewItem>>.Success(lastPreviousView);

        }
    }
}
