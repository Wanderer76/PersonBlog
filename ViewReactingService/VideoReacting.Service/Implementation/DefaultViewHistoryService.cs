﻿using Blog.Service.Models.Blog;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System.Net.Http.Json;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Models;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
{
    internal class DefaultViewHistoryService : IViewHistoryService
    {
        private readonly IReadWriteRepository<IUserEntity> _repository;
        private readonly ICacheService _cacheService;
        private readonly IHttpClientFactory _httpClientFactory;
        public DefaultViewHistoryService(IReadWriteRepository<IUserEntity> repository, ICacheService cacheService, IHttpClientFactory httpClientFactory)
        {
            _repository = repository;
            _cacheService = cacheService;
            _httpClientFactory = httpClientFactory;
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

        public async Task<Result<ReactionHistoryViewItem>> GetUserPostReactionAsync(Guid postId, Guid userId, Guid? blogId)
        {
            var lastView = await _repository.Get<UserPostView>()
                          .Where(x => x.UserId == userId)
                          .Where(x => x.PostId == postId)
                          .OrderByDescending(x => x.WatchedAt)
                          .Select(x => new
                          {
                              LastWatched = x.WatchedAt.DateTime,
                              WatchedTime = x.WatchedTime,
                              PostId = x.PostId
                          })
                          .FirstOrDefaultAsync();

            var reaction = await _repository.Get<PostReaction>()
                          .Where(x => x.PostId == postId && x.UserId == userId)
                          .FirstOrDefaultAsync();


            var subscription = blogId.HasValue
                ? await _repository.Get<SubscribedChanel>()
                .Where(x => x.UserId == userId && x.BlogId == blogId.Value)
                .AnyAsync()
                : false;

            var result = lastView == null && reaction == null && !subscription
                ? new()
                : new ReactionHistoryViewItem
                {
                    HasSubscription = subscription,
                    IsLike = reaction?.IsLike,
                    LastWatched = lastView?.LastWatched,
                    PostId = postId,
                    WatchedTime = lastView?.WatchedTime
                };

            return result;
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

            return Result<HistoryViewItem>.Success(lastPreviousView ?? new());
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
                .Where(x => x.UserId == userId && x.IsDelete == false)
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
