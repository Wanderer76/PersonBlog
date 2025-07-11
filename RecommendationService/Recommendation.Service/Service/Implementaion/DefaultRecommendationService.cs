﻿using Blog.Domain.Entities;
using Blog.Service.Models;
using FileStorage.Service.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Collections.Concurrent;

namespace Recommendation.Service.Service.Implementaion
{
    internal class DefaultRecommendationService : IRecommendationService
    {
        private readonly IReadRepository<IBlogEntity> _context;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICacheService _cacheService;
        public DefaultRecommendationService(IReadRepository<IBlogEntity> context, IFileStorageFactory fileStorageFactory, ICacheService cacheService)
        {
            _context = context;
            _fileStorageFactory = fileStorageFactory;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<VideoCardModel>> GetRecommendationsAsync(int page, int limit)
        {
            var newestPosts = await _context.Get<Post>()
                .Include(x => x.VideoFile)
                .Where(x => x.Type == PostType.Video)
                .Where(x => x.Visibility == PostVisibility.Public)
                .Where(x => x.VideoFile.IsProcessed == false)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.PreviewId != null)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.PreviewId,
                    x.Blog.PhotoUrl,
                    BlogId = x.Blog.Id,
                    BlogName = x.Blog.Title,
                    VideoId = x.VideoFile.Id,
                    x.ViewCount
                })
                .ToListAsync();

            var postMetadata = new ConcurrentDictionary<Guid, (string? PreviewUrl, string? ProfileUrl)>();

            var tasks = newestPosts
                .Select(post => Task.Run(async () =>
                {
                    using var fileStorage = _fileStorageFactory.CreateFileStorage();
                    var previewUrl = post.PreviewId != null ? await fileStorage.GetFileUrlAsync(post.Id, post.PreviewId) : null;
                    var profileUrl = post.PhotoUrl != null ? await fileStorage.GetFileUrlAsync(post.BlogId, post.PhotoUrl) : null;
                    postMetadata.TryAdd(post.Id, (previewUrl, profileUrl));
                }))
                .ToList();

            await Task.WhenAll(tasks);

            return newestPosts.Select(post =>
            {
                var postPreviews = postMetadata.TryGetValue(post.Id, out var postPreview);
                return new VideoCardModel
                {
                    PostId = post.Id,
                    BlogLogo = postPreview.ProfileUrl,
                    PreviewUrl = postPreview.PreviewUrl,
                    BlogName = post.BlogName,
                    Title = post.Title,
                    VideoId = post.VideoId,
                    BlogId = post.BlogId,
                    ViewCount = post.ViewCount
                };
            }).ToList();
        }

        public async Task<IEnumerable<VideoCardModel>> GetRecommendationsAsync(int page, int pageSize, Guid? currentPostId)
        {
            var recommendations = await GetRecommendationsAsync(page, pageSize);
            return recommendations.Count() > 2 && recommendations.Any(x => x.PostId == currentPostId)
                ? recommendations.Where(x => x.PostId != currentPostId)
                : recommendations;
        }

        public async Task<IEnumerable<VideoCardModel>> GetRecommendationsAsync(List<Guid> postIds)
        {

            var cachedValues = (await _cacheService.GetCachedDataAsync<VideoCardModel>(postIds.Select(x => new VideoCardModelCacheKey(x)))).ToList();

            var notCachedValues = postIds.Except(cachedValues.Select(x => x.PostId)).ToList();

            var newestPosts = await _context.Get<Post>()
                           .Include(x => x.VideoFile)
                           .Where(x => notCachedValues.Contains(x.Id))
                           .Where(x => x.Type == PostType.Video)
                           .Where(x => x.Visibility == PostVisibility.Public)
                           .Where(x => x.VideoFile.IsProcessed == false)
                           .Where(x => x.IsDeleted == false)
                           .Where(x => x.PreviewId != null)
                           .OrderByDescending(x => x.CreatedAt)
                           .Select(x => new
                           {
                               x.Id,
                               x.Title,
                               x.Description,
                               x.PreviewId,
                               x.Blog.PhotoUrl,
                               BlogId = x.Blog.Id,
                               BlogName = x.Blog.Title,
                               VideoId = x.VideoFile.Id,
                               x.ViewCount
                           })
                           .ToListAsync();

            var postMetadata = new ConcurrentDictionary<Guid, (string? PreviewUrl, string? ProfileUrl)>();

            var tasks = newestPosts
                .Select(post => Task.Run(async () =>
                {
                    using var fileStorage = _fileStorageFactory.CreateFileStorage();
                    var previewUrl = post.PreviewId != null ? await fileStorage.GetFileUrlAsync(post.Id, post.PreviewId) : null;
                    var profileUrl = post.PhotoUrl != null ? await fileStorage.GetFileUrlAsync(post.BlogId, post.PhotoUrl) : null;
                    postMetadata.TryAdd(post.Id, (previewUrl, profileUrl));
                }))
                .ToList();

            await Task.WhenAll(tasks);

            foreach (var post in newestPosts)
            {
                var postPreviews = postMetadata.TryGetValue(post.Id, out var postPreview);
                var model = new VideoCardModel
                {
                    PostId = post.Id,
                    BlogLogo = postPreview.ProfileUrl,
                    PreviewUrl = postPreview.PreviewUrl,
                    BlogName = post.BlogName,
                    Title = post.Title,
                    VideoId = post.VideoId,
                    BlogId = post.BlogId,
                    ViewCount = post.ViewCount
                };
                await _cacheService.SetCachedDataAsync(new VideoCardModelCacheKey(model.PostId), model, TimeSpan.FromMinutes(30));
                cachedValues.Add(model);
            }
            return cachedValues;
        }
    }
}
