using Blog.Domain.Entities;
using Blog.Service.Models.File;
using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;
using System;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultVideoService : IVideoService
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;
        private readonly ICacheService _cacheService;
        const int LifeTimeInMinutes = 60000;
        public DefaultVideoService(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk)
        {
            var isExits = await _context.Get<Post>()
                .Where(x => x.Id == uploadVideoChunk.PostId)
                .AnyAsync();

            if (!isExits)
            {
                return new Error("Не удалось найти пост");
            }

            var progress = new UploadVideoProgress(GuidService.GetNewGuid())
            {
                PostId = uploadVideoChunk.PostId,
                LastUploadChunkNumber = 0,
                TotalChunkCount = uploadVideoChunk.TotalChunkCount,
                TotalSize = uploadVideoChunk.TotalSize
            };

            var data = await _cacheService.GetOrAddDataAsync(progress, () =>
            {
                return Task.FromResult(progress);
            }, LifeTimeInMinutes);

            return progress;
        }

        public async Task<Result<VideoMetadata>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
        {
            var progress = await GetUploadVideoMetadata(uploadVideoChunk.FileId);

            if (progress == null)
            {
                return new Error("Не удалось получить данные о прогрессе загрузки");
            }

            var cacheKey = $"{nameof(VideoMetadata)}:{uploadVideoChunk.PostId}";

            var cacheResult = await _cacheService.GetCachedDataAsync<VideoMetadata>(cacheKey);

            if (cacheResult != null)
            {
                return cacheResult;
            }

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == uploadVideoChunk.PostId);

            if (post.Type != PostType.Video)
            {
                return new Error("Пост не является постом с видео");
            }

            var metadata = new VideoMetadata
            {
                Id = progress.Value.FileId,
                FileExtension = uploadVideoChunk.FileExtension,
                CreatedAt = DateTimeOffset.UtcNow,
                ContentType = uploadVideoChunk.ContentType,
                PostId = uploadVideoChunk.PostId,
                IsProcessed = true,
                Name = uploadVideoChunk.FileName,
                Resolution = VideoResolution.Original,
                Duration = uploadVideoChunk.Duration,
                ObjectName = string.Empty,
                Length = uploadVideoChunk.TotalSize,
                ProcessState = ProcessState.Load
            };
            await _cacheService.SetCachedDataAsync(cacheKey, metadata, TimeSpan.FromMinutes(LifeTimeInMinutes));
            return metadata;

        }

        public async Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId)
        {
            var data = await _cacheService.GetCachedDataAsync<UploadVideoProgress>(new UploadVideoProgress(fileId));
            if (data == null)
            {
                return new Error("Не удалось найти данные о загрузке файла");
            }
            return data;
        }
    }
}
